﻿namespace Aardvark.Base

open System
open System.Collections.Generic
open System.Threading
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop

#nowarn "9"

type NativeCall = nativeint * obj[]


module AMD64 =
    open System.IO

    type Register =
        | Rax = 0
        | Rcx = 1
        | Rdx = 2
        | Rbx = 3
        | Rsp = 4
        | Rbp = 5
        | Rsi = 6
        | Rdi = 7

        | R8  = 8
        | R9  = 9
        | R10 = 10
        | R11 = 11
        | R12 = 12
        | R13 = 13
        | R14 = 14
        | R15 = 15

    type Value =
        | Dword of uint32
        | Qword of uint64
        | Register of Register

    type Instruction =
        | Mov of Register * Value
        | CallRax
        | Push of offset : int * Value
        | PushReg of Register
        | PopReg of Register
        | Jmp of offset : int
        | Nop of width : int
        | Ret

    type ArgumentLocation =
        | RegisterArgument of Register
        | StackArgument

    type CallingConvention = { registers : Register[]; calleeSaved : Register[] }

    [<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
    module CallingConvention =
        
        let getArgumentLocation (arg : int) (cc : CallingConvention) =
            if arg < cc.registers.Length then RegisterArgument cc.registers.[arg]
            else StackArgument

        let tryFindRegister (arg : int) (cc : CallingConvention) =
            if arg < cc.registers.Length then Some cc.registers.[arg]
            else None

        let tryFindStackIndex (arg : int) (cc : CallingConvention) =
            if arg >= cc.registers.Length then Some (arg - cc.registers.Length)
            else None

    let windows = 
        { 
            registers = [| Register.Rcx; Register.Rdx; Register.R8; Register.R9 |] 
            calleeSaved = [|Register.R12; Register.R13; Register.R14; Register.R15 |]
        }

    let linux = 
        { 
            registers = [| Register.Rdi; Register.Rsi; Register.Rdx; Register.Rcx; Register.R8; Register.R9 |] 
            calleeSaved = [|Register.R12; Register.R13; Register.R14; Register.R15 |]
        }

    module Compiler =
        let value (o : obj) =
            match o with
                | :? nativeint as o -> Qword(uint64 o)
                | :? int64 as o -> Qword(uint64 o)
                | :? int as o -> Dword(uint32 o)
                | :? unativeint as o -> Qword(uint64 o)
                | :? uint64 as o -> Qword(o)
                | :? uint32 as o -> Dword(o)
                | :? Register as o -> Register(o)
                | _ -> failwithf "unsupported argument-type: %A" (o.GetType())

        let stackSize(v : Value) =
            match v with
                | Qword _ -> 8
                | Dword _ -> 8
                | Register _ -> 8

        let setArg (cc : CallingConvention) (index : int) (stackOffset : int) (arg : obj) =
            match CallingConvention.getArgumentLocation index cc with
                | RegisterArgument reg -> 
                    stackOffset, [ Mov(reg, value arg) ]
                | StackArgument -> 
                    let v = value arg
                    let s = stackSize v
                    stackOffset + s, [ Push(stackOffset, value arg) ]

        let call (ptr : nativeint) =
            [ 
                Mov(Register.Rax, Qword(uint64 ptr))
                CallRax
            ]

        let compileCalls (cc : CallingConvention) (dynamicArguments : int) (calls : seq<nativeint * obj[]>) =
            let res = List<Instruction>()

            for f,args in calls do
                let mutable stackOffset = 32

                for i in 0..dynamicArguments-1 do
                    let source = cc.calleeSaved.[i]
                    let target = cc.registers.[i]
                    res.Add(Mov(target, Register source))

                for i in 0..args.Length-1 do
                    let (newOffset,code) = setArg cc (dynamicArguments + i) stackOffset args.[i]
                    stackOffset <- newOffset
                    res.AddRange(code)

                res.AddRange(call f)

            res.ToArray()

        let compileCall (cc : CallingConvention) (dynamicArguments : int) (f : nativeint) (args : array<obj>) =
            compileCalls cc dynamicArguments [f,args]

        let functionProlog (cc : CallingConvention) (dynamicArguments : int) =
            if dynamicArguments > 0 then
                let pushTempArgs = 
                    List.init dynamicArguments (fun i -> [PushReg(cc.calleeSaved.[i]); Mov(cc.calleeSaved.[i], Register cc.registers.[i])])
                        |> List.concat
                    
                let pushRsp = PushReg(Register.Rsp)

                pushRsp::pushTempArgs |> List.toArray
            else
                [||]

        let functionEpilog (cc : CallingConvention) (dynamicArguments : int) =
            if dynamicArguments > 0 then
                let popTempArgs = 
                    List.init dynamicArguments (fun i -> PopReg(cc.calleeSaved.[i]))
                    
                let popRsp = PopReg(Register.Rsp)

                popTempArgs @ [popRsp] |> List.toArray
            else
                [||]

    module Decompiler =
        let rec private decompileAcc (cc : CallingConvention) (rax : Option<Value>) (args : Map<int, Value>) (calls : list<NativeCall>) (l : list<Instruction>) =
            
            match l with
                | PushReg _:: rest ->
                    decompileAcc cc rax args calls rest

                | PopReg _:: rest ->
                    decompileAcc cc rax args calls rest

                | CallRax::rest ->
                    let ptr = 
                        match rax with
                            | Some (Qword q) -> nativeint q
                            |_ -> failwith "[Decompiler] function pointer must be a QWORD"

                    let args =
                        args |> Map.toList 
                             |> List.map (fun (_,a) ->
                                match a with
                                    | Qword q -> q :> obj
                                    | Dword d -> d :> obj
                                    | Register r -> r :> obj
                                )
                             |> List.toArray

                    decompileAcc cc None Map.empty ((ptr, args)::calls) rest

                | Mov(Register.Rax, value)::rest ->
                    decompileAcc cc (Some value) args calls rest

                | Mov(reg, value)::rest ->
                    let argIndex = cc.registers |> Array.findIndex (fun r -> r = reg)
                    if argIndex >= 0 then
                        decompileAcc cc rax (Map.add argIndex value args) calls rest
                    else
                        decompileAcc cc rax args calls rest

                | Push(offset, value)::rest ->
                    let index = cc.registers.Length + offset / 8
                    decompileAcc cc rax (Map.add index value args) calls rest

                | Nop(_)::rest ->
                    decompileAcc cc rax args calls rest

                | [] | (Jmp(_) | Ret)::_ ->
                    calls

        let decompile (cc : CallingConvention) (instructions : Instruction[]) =
            let res = decompileAcc cc None Map.empty [] (Array.toList instructions) |> List.toArray
            Array.Reverse res
            res

    module Assembler =

        let private oneByteNop       = [|0x90uy|]
        let private twoByteNop       = [|0x66uy; 0x90uy|]
        let private threeByteNop     = [|0x0Fuy; 0x1Fuy; 0x00uy|]
        let private fourByteNop      = [|0x0Fuy; 0x1Fuy; 0x40uy; 0x00uy|]
        let private fiveByteNop      = [|0x0Fuy; 0x1Fuy; 0x44uy; 0x00uy; 0x00uy|]
        let private eightByteNop     = [|0x0Fuy; 0x1Fuy; 0x84uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy|]

        let private printBits (arr : byte[]) =
            let printSingleBits (b : byte) =
                for i in 0..7 do
                    printf "%d" ((b >>> (7 - i)) &&& 1uy)

            arr |> Seq.iter printSingleBits
            printfn ""

        let private immediateMov64 =
            [|
            
                // MOVQ encoding (according to AMD manual)
                // TODO: not quite clear why b=1 and w=1 for [R8:R15]
                // [- 4 -] w r x b | [- 0xB8 + id -]
                // 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0
                (* Register.Rax *) [| 0x48uy; 0xB8uy |]
                (* Register.Rcx *) [| 0x48uy; 0xB9uy |]
                (* Register.Rdx *) [| 0x48uy; 0xBAuy |]
                (* Register.Rbx *) [| 0x48uy; 0xBBuy |]
                (* Register.Rsp *) [| 0x48uy; 0xBCuy |]
                (* Register.Rbp *) [| 0x48uy; 0xBDuy |]
                (* Register.Rsi *) [| 0x48uy; 0xBEuy |]
                (* Register.Rdi *) [| 0x48uy; 0xBFuy |]
                (* Register.R8  *) [| 0x49uy; 0xB8uy |]
                (* Register.R9  *) [| 0x49uy; 0xB9uy |]
                (* Register.R10 *) [| 0x49uy; 0xBAuy |]
                (* Register.R11 *) [| 0x49uy; 0xBBuy |]
                (* Register.R12 *) [| 0x49uy; 0xBCuy |]
                (* Register.R13 *) [| 0x49uy; 0xBDuy |]
                (* Register.R14 *) [| 0x49uy; 0xBEuy |]
                (* Register.R15 *) [| 0x49uy; 0xBFuy |]
            |]

        let private immediateMov32 =
            [|
                // MOVD encoding (according to AMD manual)
                // TODO: not quite clear why b=1 for [r8d:r15d]
                // [- 4 -] w r x b | [- 0xB8 + id -]
                // 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0

                (* Register.Rax *) [| 0xB8uy |]          
                (* Register.Rcx *) [| 0xB9uy |]          
                (* Register.Rdx *) [| 0xBAuy |]          
                (* Register.Rbx *) [| 0xBBuy |]          
                (* Register.Rsp *) [| 0xBCuy |]          
                (* Register.Rbp *) [| 0xBDuy |]          
                (* Register.Rsi *) [| 0xBEuy |]          
                (* Register.Rdi *) [| 0xBFuy |]          
                                                 
                (* Register.R8  *) [| 0x41uy; 0xB8uy |]  
                (* Register.R9  *) [| 0x41uy; 0xB9uy |]  
                (* Register.R10 *) [| 0x41uy; 0xBAuy |]  
                (* Register.R11 *) [| 0x41uy; 0xBBuy |]  
                (* Register.R12 *) [| 0x41uy; 0xBCuy |]  
                (* Register.R13 *) [| 0x41uy; 0xBDuy |]  
                (* Register.R14 *) [| 0x41uy; 0xBEuy |]  
                (* Register.R15 *) [| 0x41uy; 0xBFuy |]  
            |]
      
        // 41 89 FF     mov r15d,edi

        // 49 89 F6     mov r14,rsi
        // 4C 89 F6     mov rsi,r14
        // 48 89 E5     mov rbp,rsp
        // 49 89 D1     mov r9,rdx
        // 49 89 D5     mov r13,rdx
        // 4C 89 EA     mov rdx,r13
        // 48 89 C2     mov rdx,rax 


        // 49 89 D1     mov r9,rdx
        // 49 89 D5     mov r13,rdx
        // D1 => 11 010(rdx) 001
        // D5 => 11 010(rdx) 101

        let private rexPrefix (wide : bool) (r : byte) (b : byte) =
            let w = if wide then 1uy else 0uy

            0x40uy ||| (w <<< 3) ||| (r <<< 2) ||| b

        let private assembleRegMov (wide : bool) (left : Register) (right : Register) (stream : BinaryWriter) =
            let left = int left |> byte
            let right = int right |> byte

            let r, left = if left >= 8uy then 1uy,left-8uy else 0uy,left
            let b, right = if right >= 8uy then 1uy,right-8uy else 0uy,right
            let w = if wide then 1uy else 0uy

            // MOV   reg/mem64, reg64       89/r
            // MOV   reg64, reg/mem64       8B/r

            // AMD Manual Volume 3 Page 21
            // ModRM layout:
            // mod |  left  | right
            // 7 6    5 4 3   2 1 0

            // REX prefix:
            // [- 4 -] w r x b | [- 0xB8 + id -]
            // 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0

            let opCode = 0x40uy ||| (w <<< 3) ||| (r <<< 2) ||| b
            let modRM = 0xC0uy ||| (left <<< 3) ||| right


            stream.Write [|opCode; 0x89uy; modRM|]

        let private asssemblePushReg (wide : bool) (reg : Register) (stream : BinaryWriter) =
            let reg = reg |> int |> byte
            let w = if wide then 1uy else 0uy
            let b, reg = if reg >= 8uy then 1uy,reg-8uy else 0uy,reg

            let code = 0x50uy + reg
            let rex = rexPrefix wide 0uy b

            stream.Write [|rex; code|]

        let private asssemblePopReg (wide : bool) (reg : Register) (stream : BinaryWriter) =
            let reg = reg |> int |> byte
            let w = if wide then 1uy else 0uy
            let b, reg = if reg >= 8uy then 1uy,reg-8uy else 0uy,reg

            let code = 0x58uy + reg
            let rex = rexPrefix wide 0uy b

            stream.Write [|rex; code|]


        // base -> base: 
        // 0x48uy 0x89uy


        let assembleMov (target : Register) (value : Value) (stream : BinaryWriter) =
            match value with
                | Qword q -> 
                    stream.Write immediateMov64.[int target]
                    stream.Write q 
                | Dword d ->
                    stream.Write immediateMov32.[int target]
                    stream.Write d
                | Register r ->
                    stream |> assembleRegMov true r target

        let assemblePush (stackOffset : int) (value : Value) (stream : BinaryWriter) =
            stream |> assembleMov Register.Rax value
            stream.Write([| 0x48uy; 0x89uy; 0x44uy; 0x24uy; byte stackOffset |])

        let assembleCallRax (stream : BinaryWriter) =
            stream.Write [|0xFFuy; 0xD0uy|]

        let assembleJmp (offset : int) (stream : BinaryWriter) =
            stream.Write 0xE9uy
            stream.Write offset

        let rec assembleNop (width : int) (stream : BinaryWriter) =
            if width > 0 then
                match width with
                    | 1 -> stream.Write oneByteNop
                    | 2 -> stream.Write twoByteNop
                    | 3 -> stream.Write threeByteNop
                    | 4 -> stream.Write fourByteNop
                    | 5 -> stream.Write fiveByteNop
                    | 6 -> stream.Write threeByteNop; stream.Write threeByteNop // TODO: find good 6 byte nop sequence
                    | 7 -> stream.Write fourByteNop; stream.Write threeByteNop // TODO: find good 7 byte nop sequence
                    | _ -> stream.Write eightByteNop; assembleNop (width - 8) stream

        let assembleRet (stream : BinaryWriter) =
            stream.Write 0xC3uy

        let assembleInstruction (i : Instruction) (stream : BinaryWriter) =
            match i with
                | Mov(r, value)       -> stream |> assembleMov r value
                | Push(offset, value) -> stream |> assemblePush offset value
                | CallRax             -> stream |> assembleCallRax
                | Ret                 -> stream |> assembleRet
                | Jmp o               -> stream |> assembleJmp o
                | Nop w               -> stream |> assembleNop w
                | PushReg r           -> stream |> asssemblePushReg true r
                | PopReg r            -> stream |> asssemblePopReg true r

        let assembleTo (stream : Stream) (instructions : Instruction[]) =
            let writer = new BinaryWriter(stream, Text.ASCIIEncoding.ASCII, true)
            for i in instructions do
                writer |> assembleInstruction i

        let assemble (instructions : Instruction[]) =
            use s = new MemoryStream()
            instructions |> assembleTo s
            s.Flush()
            s.ToArray()

    module Disassembler =
        
        let fail (current : list<Instruction>) = current

        let rec private disassembleAcc (current : list<Instruction>) (r : BinaryReader) =
            if r.BaseStream.Position = r.BaseStream.Length then
                current
            else
                let b0 = r.ReadByte()

                match b0 with
                    // NOP 1
                    | 0x90uy -> 
                        disassembleAcc ((Nop 1)::current) r

                    | 0x66uy ->
                        if r.ReadByte() = 0x90uy then disassembleAcc ((Nop 2)::current) r
                        else fail current

                    | 0x0Fuy ->
                        let b = r.ReadBytes(2)
                        match b with
                            | [| 0x1Fuy; 0x00uy |] -> 
                                disassembleAcc ((Nop 3)::current) r

                            | [| 0x1Fuy; 0x40uy |] -> 
                                r.ReadByte() |> ignore
                                disassembleAcc ((Nop 4)::current) r

                            | [| 0x1Fuy; 0x44uy |] -> 
                                r.ReadBytes(2) |> ignore
                                disassembleAcc ((Nop 5)::current) r

                            | [| 0x1Fuy; 0x84uy |] -> 
                                r.ReadBytes(5) |> ignore
                                disassembleAcc ((Nop 8)::current) r

                            | _ ->
                                fail current

                    // MOVQ lo registers
                    | 0x48uy -> 
                        let b1 = r.ReadByte()
                        match b1 with
                            | 0x89uy ->
                                if r.ReadBytes(2) = [|0x44uy; 0x24uy|] then
                                    let stackOffset = r.ReadByte() |> int
                                    match current with
                                        | Mov(Register.Rax, v)::rest ->
                                            disassembleAcc (Push(stackOffset, v)::rest) r
                                        | _ ->
                                            fail current

                                else
                                    fail current
                            | _ ->
                                let reg = b1 - 0xB8uy |> int |> unbox<Register>
                                let arg = r.ReadUInt64() |> Qword
                                disassembleAcc (Mov(reg, arg)::current) r

                    // MOVQ hi registers
                    | 0x49uy -> 
                        let reg = r.ReadByte() - 0xB8uy + 8uy |> int |> unbox<Register>
                        let arg = r.ReadUInt64() |> Qword
                        disassembleAcc (Mov(reg, arg)::current) r

                    // MOVD lo registers
                    | reg when reg >= 0xB8uy && reg <= 0xBFuy ->
                        let reg = reg - 0xB8uy |> int |> unbox<Register>
                        let arg = r.ReadUInt32() |> Dword
                        disassembleAcc (Mov(reg, arg)::current) r

                    // MOVD hi registers
                    | 0x41uy ->
                        let reg = r.ReadByte() - 0xB8uy + 8uy |> int |> unbox<Register>
                        let arg = r.ReadUInt32() |> Dword
                        disassembleAcc (Mov(reg, arg)::current) r

                    // CALL
                    | 0xFFuy ->
                        if r.ReadByte() = 0xD0uy then disassembleAcc (CallRax::current) r
                        else fail current

                    | 0xE9uy ->
                        let dist = r.ReadInt32()
                        (Jmp(dist)::current)

                    | _ ->
                        fail current


        let disassembleFrom (r : BinaryReader) =
            let res = disassembleAcc [] r

            let rec flattenNops (l : list<Instruction>) =
                match l with
                    | (Nop l)::(Nop r)::rest -> flattenNops <| (Nop (l+r))::rest
                    | i::rest -> i::flattenNops rest
                    | [] -> []

            res |> List.rev |> flattenNops |> List.toArray


        let disassemble (arr : byte[]) =
            use ms = new MemoryStream(arr)
            let reader = new BinaryReader(ms)
            disassembleFrom reader


module ASM =
    let os = System.Environment.OSVersion
    type Architecture = AMD64 | X86 | ARM

    let (|Windows|Linux|Mac|) (p : System.OperatingSystem) =
        match p.Platform with
            | System.PlatformID.Unix -> Linux
            | System.PlatformID.MacOSX -> Mac
            | _ -> Windows

    let private getLinuxCpuArchitecture() =
        
        let ps = System.Diagnostics.ProcessStartInfo("lscpu", "")
        ps.UseShellExecute <- false
        ps.RedirectStandardOutput <- true
        let proc = System.Diagnostics.Process.Start(ps)
        proc.WaitForExit()
        let cpu = proc.StandardOutput.ReadToEnd()
        let rx = System.Text.RegularExpressions.Regex @"Architecture[ \t]*:[ \t]*(?<arch>.*)"
        let m = rx.Match cpu

        if m.Success then
            let arch = m.Groups.["arch"].Value
            match arch with
                | "x86_64" -> AMD64
                | "x86" -> AMD64
                | _ -> 
                    if arch.Contains "arm" then ARM
                    else failwithf "unknown architecture: %A" arch
        else
            failwith "could not determine cpu info"
    
    let cpu = 
        match os with
            | Windows -> 
                match System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") with
                    | "AMD64" -> AMD64
                    | _ -> X86
            | Linux ->
                getLinuxCpuArchitecture()
            | Mac ->
                failwith "mac currently not supported" 

    let functionProlog =
        let specific =
            match os, cpu with
                | Windows, AMD64 -> 
                    fun  (maxArgs : int) ->
                        let additionalSize =
                            if maxArgs < 5 then 8uy
                            else 8 * maxArgs - 24 |> byte
                        [| 0x48uy; 0x83uy; 0xECuy; 0x20uy + additionalSize |]

                | Linux, AMD64 -> fun (maxArgs : int) -> [||]
                | Mac, AMD64 -> fun (maxArgs : int) -> [||]
                | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu    
              
        match cpu with  
            | AMD64 ->
                let cc = match os with | Windows -> AMD64.windows | _ -> AMD64.linux

                fun (dynamicArgs : int) (maxArgs : int) ->
                    let real = specific maxArgs
                    let prefix = AMD64.Compiler.functionProlog cc dynamicArgs |> AMD64.Assembler.assemble
                    Array.append real prefix

            | _ ->
                failwithf "no assembler for: %A / %A" os.Platform cpu    
                
    let functionEpilog =
        let specific =
            match os, cpu with
                | Windows, AMD64 ->
                    fun (maxArgs : int) ->
                        let additionalSize =
                            if maxArgs < 5 then 8uy
                            else 8 * maxArgs - 24 |> byte
                        [| 0x48uy; 0x83uy; 0xC4uy; 0x20uy + additionalSize; 0xC3uy|]
                | Linux, AMD64 -> fun (maxArgs : int) -> [|0xC3uy|]
                | Mac, AMD64 -> fun (maxArgs : int) -> [|0xC3uy|]
                | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu     
                 
        match cpu with  
            | AMD64 ->
                let cc = match os with | Windows -> AMD64.windows | _ -> AMD64.linux

                fun (dynamicArgs : int) (maxArgs : int) ->
                    let real = specific maxArgs
                    let prefix = AMD64.Compiler.functionEpilog cc dynamicArgs |> AMD64.Assembler.assemble
                    Array.append prefix real

            | _ ->
                failwithf "no assembler for: %A / %A" os.Platform cpu    

    let private compileCallsNative : int -> seq<NativeCall> -> byte[] =
         match os, cpu with
            | Windows, AMD64 -> 
                fun dyn calls -> 
                    AMD64.Compiler.compileCalls AMD64.windows dyn calls |> AMD64.Assembler.assemble
            | (Linux | Mac), AMD64 -> 
                fun dyn calls -> 
                    AMD64.Compiler.compileCalls AMD64.linux dyn calls |> AMD64.Assembler.assemble
            | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu      

    let jumpSize =
        match cpu with
            | AMD64 -> 5
            | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu      

    let jumpArgumentOffset =
        match cpu with
            | AMD64 -> 1
            | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu      

    let jumpArgumentAlign =
        match cpu with
            | AMD64 -> 4
            | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu      

    let assembleJump (nopBytes : int) (offset : int) =
         match cpu with
            | AMD64 -> AMD64.Assembler.assemble [|AMD64.Nop nopBytes; AMD64.Jmp offset|]
            | _ -> failwithf "no assembler for: %A / %A" os.Platform cpu      

    let assembleCalls (dynamicArguments : int) (calls : #seq<NativeCall>) =
        calls |> compileCallsNative dynamicArguments

    let disassemble : byte[] -> NativeCall[] =
        match cpu with
            | AMD64 -> 
                let cc =
                    match os with
                        | Windows -> AMD64.windows
                        | Linux -> AMD64.linux
                        | Mac -> AMD64.linux

                fun (data : byte[]) ->
                    data |> AMD64.Disassembler.disassemble |> AMD64.Decompiler.decompile cc


            | _ -> failwithf "no disassembler for: %A / %A" os.Platform cpu      

    let encodedJumpSize = jumpSize + (jumpArgumentAlign - 1)
 

[<AllowNullLiteral>]
type CodeFragment(memory : managedptr, containsJmp : bool) = 
    let mutable memory = memory
    let mutable containsJmp = containsJmp

    let startOffsets = List<int>()

    let resetStartOffsets() =
        startOffsets.[0] <- 0
        if startOffsets.Count > 1 then
            startOffsets.RemoveRange(1, startOffsets.Count - 1)

    do startOffsets.Add(0)

    let nonAlignedJumpOffset() =
        memory.Size - ASM.encodedJumpSize
        
    let alignedJumpArgumentOffset() =
        let jmpStart = ASM.jumpArgumentOffset + nonAlignedJumpOffset()
        let a = ASM.jumpArgumentAlign - 1
        (jmpStart + a) &&& ~~~a

    member x.Calls =
        memory 
            |> ManagedPtr.readArray 0
            |> ASM.disassemble

    member x.Offset =
        memory.Offset

    member x.Memory : managedptr =
        memory

    member x.ContainsJmp = containsJmp


    member x.WriteNextPointer(target : nativeint) =
        let jmpArgOffset = alignedJumpArgumentOffset()

        let jmpEndLocation = memory.Offset + nativeint (jmpArgOffset + 4)
        let jmpArg = int (target - jmpEndLocation)

        if containsJmp then
            // atomic write here since the offset is 4-byte aligned
            memory |> ManagedPtr.write jmpArgOffset jmpArg
        else
            // if (for some reason) the fragment does currently not end
            // with a jmp instruction we need to re-create it
            let jmpStart = nonAlignedJumpOffset()
            let nopSize = (jmpArgOffset - 1) - jmpStart

            let jmp = ASM.assembleJump nopSize jmpArg
            memory |> ManagedPtr.writeArray jmpStart jmp
            containsJmp <- true

        let distance = target - (memory.Offset + nativeint memory.Size) |> int
        abs distance

    member x.ReadNextPointer() =
        if containsJmp then
            let jmpArgOffset = alignedJumpArgumentOffset()
            let jmpArg : int = memory |> ManagedPtr.read jmpArgOffset
                
            let nextPtr = 
                memory.Offset +                             // where am i located
                nativeint (jmpArgOffset + sizeof<int>) +    // where is the jmp instruction
                nativeint jmpArg                            // what's the argument of the jmp instruction

            nextPtr
        else
            -1n

    member x.Write (calls : NativeCall[]) =
        resetStartOffsets()
        calls |> ASM.assembleCalls 0 |> x.Write

    member x.Write (binary : byte[]) =
        resetStartOffsets()
        let size = binary.Length + ASM.encodedJumpSize

        if size <> memory.Size then
            let moved =  memory |> ManagedPtr.realloc size

            // patch our own next-pointer since its location was changed
            containsJmp <- false
            //x.FixJumps moved

        memory |> ManagedPtr.writeArray 0 binary


    member x.Append (data : byte[]) =
        let id = startOffsets.Count - 1
        let oldSize = memory.Size
        let newSize = oldSize + data.Length

        // realloc the memory
        let moved = memory |> ManagedPtr.realloc newSize
        containsJmp <- false

        // write the data to the new region
        memory |> ManagedPtr.writeArray (oldSize - ASM.encodedJumpSize) data

        // if the block moved update the jump
        //x.FixJumps moved

        startOffsets.Add (newSize - ASM.encodedJumpSize)
        id

    member x.Update(id : int, data : byte[]) =
        let startOffset = startOffsets.[id]
        let endOffset = startOffsets.[id + 1]
        let length = endOffset - startOffset

        if length = data.Length then
            // if the length did not change simply write the new data
            memory |> ManagedPtr.writeArray startOffset data
        else
            let restSize = memory.Size - endOffset
            let delta = length - data.Length


            let moved = 
                if delta > 0 then
                    let moved = memory |> ManagedPtr.realloc (memory.Size + delta)
                    // move all subsequent sub-blocks to the right (making space for data)
                    memory |> ManagedPtr.move endOffset (endOffset + delta) restSize
                    moved
                else
                    // move all subsequent sub-blocks to the left
                    memory |> ManagedPtr.move endOffset (endOffset + delta) restSize
                    memory |> ManagedPtr.realloc (memory.Size + delta)
            containsJmp <- false

            // write the data
            memory |> ManagedPtr.writeArray startOffset data

            // adjust all subsequent offsets
            for i in id+1..startOffsets.Count-1 do
                startOffsets.[i] <- startOffsets.[i] + delta


    member x.Remove(id : int) =
        x.Update(id, [||])

    member x.Clear() =
        resetStartOffsets()
        let moved = memory |> ManagedPtr.realloc ASM.encodedJumpSize 
        containsJmp <- false

    member x.Dispose() =
        if not (isNull memory) then
            ManagedPtr.free memory
            memory <- null
            containsJmp <- false



    new(manager : MemoryManager, content : byte[]) =
        let ptr = manager.Alloc(content.Length + ASM.encodedJumpSize)
        ptr |> ManagedPtr.writeArray 0 content
        CodeFragment(ptr, false)

    new(manager : MemoryManager, calls : NativeCall[]) = new CodeFragment(manager, calls |> ASM.assembleCalls 0)
    new(manager : MemoryManager) = new CodeFragment(manager, ([||] : byte[]))


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CodeFragment =

    let inline prolog (maxArgs : int) (m : MemoryManager) =
        new CodeFragment(m, ASM.functionProlog 0 maxArgs)

    let inline epilog (maxArgs : int) (m : MemoryManager) =
        new CodeFragment(m, ASM.functionEpilog 0 maxArgs)

    let inline ofCalls (calls : seq<NativeCall>) (m : MemoryManager) =
        new CodeFragment(m, Seq.toArray calls)

    let inline update (calls : seq<NativeCall>) (f : CodeFragment) =
        f.Write (Seq.toArray calls)

    let inline destroy (f : CodeFragment) =
        f.Dispose()

    let inline offset (f : CodeFragment) =
        f.Offset

    let usePointer (f : CodeFragment) (func : nativeint -> 'a) =
        let manager = f.Memory.Parent
        ReaderWriterLock.read manager.PointerLock (fun () ->
            let ptr = manager.Pointer + f.Offset
            func ptr
        )

    let wrap (f : CodeFragment) : unit -> unit =
        let current = ref 0n
        let run = ref id

        fun () ->
            usePointer f (fun p ->
                if p <> !current then
                    current := p
                    run := UnmanagedFunctions.wrap p

                (!run)()
            )