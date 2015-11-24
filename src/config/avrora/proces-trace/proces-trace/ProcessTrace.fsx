#r "binaries/FSharp.Data/FSharp.Data.dll"
#r "binaries/fspickler.1.5.2/lib/net45/FsPickler.dll"

#load "Datatypes.fsx"
#load "AVR.fsx"

open System
open System.IO
open System.Linq
open System.Text.RegularExpressions
open System.Runtime.Serialization
open FSharp.Data
open Nessos.FsPickler
open Datatypes

type RtcdataXml = XmlProvider<"rtcdata-example.xml", Global=true>
type Rtcdata = RtcdataXml.Methods
type MethodImpl = RtcdataXml.MethodImpl
type ProfilerdataXml = XmlProvider<"profilerdata-example.xml", Global=true>
type Profilerdata = ProfilerdataXml.ExecutionCountPerInstruction
type ProfiledInstruction = ProfilerdataXml.Instruction
type DarjeelingInfusionHeaderXml = XmlProvider<"infusionheader-example.dih", Global=true>
type Dih = DarjeelingInfusionHeaderXml.Dih

let JvmInstructionFromXml (xml : RtcdataXml.JavaInstruction) =
    {
        JvmInstruction.index = xml.Index;
        text = xml.Text;
    }
let AvrInstructionFromXml (xml : RtcdataXml.AvrInstruction) =
    {
        AvrInstruction.address = Convert.ToInt32(xml.Address.Trim(), 16);
        opcode = Convert.ToInt32((xml.Opcode.Trim()), 16);
        text = xml.Text;
    }

let jvmOpcodeCategories = 
    [("01) Ref stack ld/st", ["JVM_ALOAD"; "JVM_ALOAD_0"; "JVM_ALOAD_1"; "JVM_ALOAD_2"; "JVM_ALOAD_3"; "JVM_ASTORE"; "JVM_ASTORE_0"; "JVM_ASTORE_1"; "JVM_ASTORE_2"; "JVM_ASTORE_3"; "JVM_GETFIELD_A"; "JVM_PUTFIELD_A"; "JVM_GETSTATIC_A"; "JVM_PUTSTATIC_A"]);
     ("02) Int stack ld/st", ["JVM_SLOAD"; "JVM_SLOAD_0"; "JVM_SLOAD_1"; "JVM_SLOAD_2"; "JVM_SLOAD_3"; "JVM_ILOAD"; "JVM_ILOAD_0"; "JVM_ILOAD_1"; "JVM_ILOAD_2"; "JVM_ILOAD_3"; "JVM_SSTORE"; "JVM_SSTORE_0"; "JVM_SSTORE_1"; "JVM_SSTORE_2"; "JVM_SSTORE_3"; "JVM_ISTORE"; "JVM_ISTORE_0"; "JVM_ISTORE_1"; "JVM_ISTORE_2"; "JVM_ISTORE_3"; "JVM_IPOP"; "JVM_IPOP2"; "JVM_IDUP"; "JVM_IDUP2"; "JVM_IDUP_X"; "JVM_APOP"; "JVM_ADUP"; "JVM_GETFIELD_B"; "JVM_GETFIELD_C"; "JVM_GETFIELD_S"; "JVM_GETFIELD_I"; "JVM_PUTFIELD_B"; "JVM_PUTFIELD_C"; "JVM_PUTFIELD_S"; "JVM_PUTFIELD_I"; "JVM_GETSTATIC_B"; "JVM_GETSTATIC_C"; "JVM_GETSTATIC_S"; "JVM_GETSTATIC_I"; "JVM_PUTSTATIC_B"; "JVM_PUTSTATIC_C"; "JVM_PUTSTATIC_S"; "JVM_PUTSTATIC_I"]);
     ("03) Constant load", ["JVM_SCONST_M1"; "JVM_SCONST_0"; "JVM_SCONST_1"; "JVM_SCONST_2"; "JVM_SCONST_3"; "JVM_SCONST_4"; "JVM_SCONST_5"; "JVM_ICONST_M1"; "JVM_ICONST_0"; "JVM_ICONST_1"; "JVM_ICONST_2"; "JVM_ICONST_3"; "JVM_ICONST_4"; "JVM_ICONST_5"; "JVM_ACONST_NULL"; "JVM_BSPUSH"; "JVM_BIPUSH"; "JVM_SSPUSH"; "JVM_SIPUSH"; "JVM_IIPUSH"; "JVM_LDS"]);
     ("04) Array ld/st", ["JVM_BALOAD"; "JVM_CALOAD"; "JVM_SALOAD"; "JVM_IALOAD"; "JVM_AALOAD"; "JVM_BASTORE"; "JVM_CASTORE"; "JVM_SASTORE"; "JVM_IASTORE"; "JVM_AASTORE"]);
     ("05) Branches", ["JVM_SIFEQ"; "JVM_SIFNE"; "JVM_SIFLT"; "JVM_SIFGE"; "JVM_SIFGT"; "JVM_SIFLE"; "JVM_IIFEQ"; "JVM_IIFNE"; "JVM_IIFLT"; "JVM_IIFGE"; "JVM_IIFGT"; "JVM_IIFLE"; "JVM_IFNULL"; "JVM_IFNONNULL"; "JVM_IF_SCMPEQ"; "JVM_IF_SCMPNE"; "JVM_IF_SCMPLT"; "JVM_IF_SCMPGE"; "JVM_IF_SCMPGT"; "JVM_IF_SCMPLE"; "JVM_IF_ICMPEQ"; "JVM_IF_ICMPNE"; "JVM_IF_ICMPLT"; "JVM_IF_ICMPGE"; "JVM_IF_ICMPGT"; "JVM_IF_ICMPLE"; "JVM_IF_ACMPEQ"; "JVM_IF_ACMPNE"; "JVM_GOTO"; "JVM_TABLESWITCH"; "JVM_LOOKUPSWITCH"; "JVM_BRTARGET" ]);
     ("06) Math", ["JVM_SADD"; "JVM_SSUB"; "JVM_SMUL"; "JVM_SDIV"; "JVM_SREM"; "JVM_SNEG"; "JVM_IADD"; "JVM_ISUB"; "JVM_IMUL"; "JVM_IDIV"; "JVM_IREM"; "JVM_INEG"; "JVM_IINC"; "JVM_IINC_W"]);
     ("07) Bit shifts", ["JVM_SSHL"; "JVM_SSHR"; "JVM_SUSHR"; "JVM_ISHL"; "JVM_ISHR"; "JVM_IUSHR"]);
     ("08) Bit logic", ["JVM_SAND"; "JVM_SOR"; "JVM_SXOR"; "JVM_IAND"; "JVM_IOR"; "JVM_IXOR"]);
     ("09) Conversions", ["JVM_S2B"; "JVM_S2C"; "JVM_S2I"; "JVM_I2S"]);
     ("10) Others", ["JVM_NOP"; "JVM_SRETURN"; "JVM_IRETURN"; "JVM_ARETURN"; "JVM_RETURN"; "JVM_INVOKEVIRTUAL"; "JVM_INVOKESPECIAL"; "JVM_INVOKESTATIC"; "JVM_INVOKEINTERFACE"; "JVM_NEW"; "JVM_NEWARRAY"; "JVM_ANEWARRAY"; "JVM_ARRAYLENGTH"; "JVM_CHECKCAST"; "JVM_INSTANCEOF"])] in
let getCategoryForJvmOpcode opcode =
    match jvmOpcodeCategories |> List.tryFind (fun (cat, opcodes) -> opcodes |> List.exists ((=) opcode)) with
    | Some(cat, _) -> cat
    | None -> "11) ????"
let getAllJvmOpcodeCategories =
    jvmOpcodeCategories |> List.map (fun (cat, opcodes) -> cat)


// Input: the optimised avr code, and a list of tuples of unoptimised avr instructions and the jvm instruction that generated them
// Returns: a list of tuples (optimised avr instruction, unoptimised avr instruction, corresponding jvm index)
//          the optimised avr instruction may be None for instructions that were removed completely by the optimiser
let rec matchOptUnopt (optimisedAvr : AvrInstruction list) (unoptimisedAvr : (AvrInstruction*JvmInstruction) list) =
    let isAOT_PUSH x = AVR.is AVR.PUSH (x.opcode) || AVR.is AVR.ST_XINC (x.opcode) // AOT uses 2 stacks
    let isAOT_POP x = AVR.is AVR.POP (x.opcode) || AVR.is AVR.LD_DECX (x.opcode)
    let isMOV x = AVR.is AVR.MOV (x.opcode)
    let isMOVW x = AVR.is AVR.MOVW (x.opcode)
    let isBREAK x = AVR.is AVR.BREAK (x.opcode)
    let isNOP x = AVR.is AVR.NOP (x.opcode)
    let isJMP x = AVR.is AVR.JMP (x.opcode)
    let isRJMP x = AVR.is AVR.RJMP (x.opcode)
    let isBRANCH x = AVR.is AVR.BREQ (x.opcode)|| AVR.is AVR.BRGE (x.opcode)|| AVR.is AVR.BRLT (x.opcode)|| AVR.is AVR.BRNE (x.opcode)
    let isMOV_MOVW_PUSH_POP x = isMOV x || isMOVW x || isAOT_PUSH x || isAOT_POP x
    match optimisedAvr, unoptimisedAvr with
    // Identical instructions: match and consume both
    | optimisedHead :: optimisedTail, (unoptimisedHead, jvmHead) :: unoptTail when optimisedHead.text = unoptimisedHead.text
        -> (Some optimisedHead, unoptimisedHead, jvmHead) :: matchOptUnopt optimisedTail unoptTail
    // Match a MOV to a single PUSH instruction (bit arbitrary whether to count the cycle for the PUSH or POP that was optimised)
    | optMOV :: optTail, (unoptPUSH, jvmHead) :: unoptTail when isMOV(optMOV) && isAOT_PUSH(unoptPUSH)
        -> (Some optMOV, unoptPUSH, jvmHead)
            :: matchOptUnopt optTail unoptTail
    // Match a MOVW to two PUSH instructions (bit arbitrary whether to count the cycle for the PUSH or POP that was optimised)
    | optMOVW :: optTail, (unoptPUSH1, jvmHead1) :: (unoptPUSH2, jvmHead2) :: unoptTail when isMOVW(optMOVW) && isAOT_PUSH(unoptPUSH1) && isAOT_PUSH(unoptPUSH2)
        -> (Some optMOVW, unoptPUSH1, jvmHead1)
            :: (None, unoptPUSH2, jvmHead2)
            :: matchOptUnopt optTail unoptTail
    // If the unoptimised head is a MOV PUSH or POP, skip it
    | _, (unoptimisedHead, jvmHead) :: unoptTail when isMOV_MOVW_PUSH_POP(unoptimisedHead)
        -> (None, unoptimisedHead, jvmHead) :: matchOptUnopt optimisedAvr unoptTail
    // BREAK signals a branchtag that would have been replaced in the optimised code by a
    // branch to the real address, possibly followed by one or two NOPs
    | _, (unoptBranchtag, jvmBranchtag) :: (unoptBranchtag2, jvmBranchtag2) :: unoptTail when isBREAK(unoptBranchtag)
        -> match optimisedAvr with
            // Short conditional jump
            | optBR :: optNOP :: optNOP2 :: optTail when isBRANCH(optBR) && isNOP(optNOP) && isNOP(optNOP2)
                -> (Some optBR, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP2, unoptBranchtag, jvmBranchtag)
                    :: matchOptUnopt optTail unoptTail
            // Mid range conditional jump
            | optBR :: optRJMP :: optNOP :: optTail when isBRANCH(optBR) && isRJMP(optRJMP) && isNOP(optNOP)
                -> (Some optBR, unoptBranchtag, jvmBranchtag)
                    :: (Some optRJMP, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP, unoptBranchtag, jvmBranchtag)
                    :: matchOptUnopt optTail unoptTail
            // Long conditional jump
            | optBR :: optJMP :: optTail when isBRANCH(optBR) && isJMP(optJMP)
                -> (Some optBR, unoptBranchtag, jvmBranchtag)
                    :: (Some optJMP, unoptBranchtag, jvmBranchtag)
                    :: matchOptUnopt optTail unoptTail
            // Uncondtional mid range jump
            | optRJMP :: optNOP :: optNOP2 :: optTail when isRJMP(optRJMP) && isNOP(optNOP) && isNOP(optNOP2)
                -> (Some optRJMP, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP2, unoptBranchtag, jvmBranchtag)
                    :: matchOptUnopt optTail unoptTail
            // Uncondtional long jump
            | optJMP :: optNOP :: optTail when isJMP(optJMP) && isNOP(optNOP)
                -> (Some optJMP, unoptBranchtag, jvmBranchtag)
                    :: (Some optNOP, unoptBranchtag, jvmBranchtag)
                    :: matchOptUnopt optTail unoptTail
            | _ -> failwith "Incorrect branctag"
    | [], [] -> [] // All done.
    | _, _ -> failwith "Some instructions couldn't be matched"

let countersForAddress (profilerdata : ProfiledInstruction list) address =
    let profiledInstruction = profilerdata |> List.find (fun x -> Convert.ToInt32(x.Address.Trim(), 16) = address) in
     { executions = profiledInstruction.Executions; cycles = (profiledInstruction.Cycles+profiledInstruction.CyclesSubroutine) }

// Input: the original Java instructions, trace data from avrora profiler, and the output from matchOptUnopt
// Returns: a list of Result records, showing the optimised code per original JVM instructions, and amount of cycles spent per optimised instruction
let addCycles (jvmInstructions : JvmInstruction list) (profilerdata : ProfiledInstruction list) (matchedResults : (AvrInstruction option*AvrInstruction*JvmInstruction) list) =
    jvmInstructions |> List.map
        (fun jvm ->
            let resultsForThisJvm = matchedResults |> List.filter (fun b -> let (_, _, jvm2) = b in jvm.index = jvm2.index) in
            let resultsWithCycles = resultsForThisJvm |> List.map (fun (opt, unopt, _) ->
                  let counters = match opt with
                                 | None -> ExecCounters.empty
                                 | Some(optValue) -> countersForAddress profilerdata optValue.address
                  { unopt = unopt; opt = opt; counters = counters }) in
            let avrCountersToJvmCounters a b =
                { cycles = a.cycles+b.cycles; executions = (if a.executions > 0 then a.executions else b.executions) }
            { jvm = jvm
              avr = resultsWithCycles
              counters = resultsWithCycles |> List.map (fun r -> r.counters) |> List.fold (avrCountersToJvmCounters) ExecCounters.empty })

let countPushPopMovw (results : ResultJava list) =
    let isAOT_PUSH x = AVR.is AVR.PUSH (x.opcode) || AVR.is AVR.ST_XINC (x.opcode) // AOT uses 2 stacks
    let isAOT_POP x = AVR.is AVR.POP (x.opcode) || AVR.is AVR.LD_DECX (x.opcode)
    let isMOVW x = AVR.is AVR.MOVW (x.opcode)
    let sumForOpcode predicate =
        results |> List.map (fun r -> r.avr)
                |> List.concat
                |> List.map (fun avr -> match avr.opt with
                                        | Some(avropt) when predicate avropt
                                          -> avr.counters
                                        | _
                                          -> ExecCounters.empty)
                |> List.fold (+) ExecCounters.empty
    (sumForOpcode isAOT_PUSH, sumForOpcode isAOT_POP, sumForOpcode isMOVW)

let getTimersFromStdout (stdoutlog : string list) =
    let pattern = "timer number (10\d): (\d+) cycles"
    stdoutlog |> List.map (fun line -> Regex.Match(line, pattern))
              |> List.filter (fun regexmatch -> regexmatch.Success)
              |> List.map (fun regexmatch -> (regexmatch.Groups.[1].Value, Int32.Parse(regexmatch.Groups.[2].Value)))
              |> List.map (fun (timer, cycles) ->
                                match timer with
                                | "101" -> ("C", cycles)
                                | "102" -> ("AOT", cycles)
                                | "103" -> ("Java", cycles)
                                | _ -> ("", cycles))
              |> List.sortBy (fun (timer, cycles) -> match timer with "C" -> 1 | "AOT" -> 2 | "Java" -> 3 | _ -> 4)

let getNativeInstructionsFromObjdump (objdumpOutput : string list) (profilerdata : ProfiledInstruction list) =
    let startIndex = objdumpOutput |> List.findIndex (fun line -> Regex.IsMatch(line, "^[0-9a-fA-F]+ <rtcbenchmark_measure_native_performance>:$"))
    let disasmTail = objdumpOutput |> List.skip (startIndex + 1)
    let endIndex = disasmTail |> List.findIndex (fun line -> Regex.IsMatch(line, "^[0-9a-fA-F]+ <.*>:$"))
    let disasm = disasmTail |> List.take endIndex |> List.filter ((<>) "")
    let pattern = "^\s*([0-9a-fA-F]+):((\s[0-9a-fA-F][0-9a-fA-F])+)\s+(\S.*)$"
    let regexmatches = disasm |> List.map (fun line -> Regex.Match(line, pattern))
    let avrInstructions = regexmatches |> List.map (fun regexmatch ->
        let opcodeBytes = regexmatch.Groups.[2].Value.Split(' ') |> Array.map (fun x -> Convert.ToInt32(x.Trim(), 16))
        let opcode = if (opcodeBytes.Length = 2)
                     then ((opcodeBytes.[1] <<< 8) + opcodeBytes.[0])
                     else ((opcodeBytes.[3] <<< 24) + (opcodeBytes.[2] <<< 16) + (opcodeBytes.[1] <<< 8) + opcodeBytes.[0])
        {
            AvrInstruction.address = Convert.ToInt32(regexmatch.Groups.[1].Value, 16);
            opcode = opcode;
            text = regexmatch.Groups.[4].Value;
        })
    avrInstructions |> List.map (fun avr -> (avr, countersForAddress profilerdata avr.address))

// Process trace main function
let processTrace benchmark (dih : Dih) (rtcdata : Rtcdata) (profilerdata : ProfiledInstruction list) (stdoutlog : string seq) (disasm : string list) =
    // Find the methodImplId for a certain method in a Darjeeling infusion header
    let findRtcbenchmarkMethodImplId (dih : Dih) methodName =
        let infusionName = dih.Infusion.Header.Name
        let methodDef = dih.Infusion.Methoddeflists |> Seq.find (fun def -> def.Name = methodName)
        let methodImpl = dih.Infusion.Methodimpllists |> Seq.find (fun impl -> impl.MethoddefEntityId = methodDef.EntityId && impl.MethoddefInfusion = infusionName)
        methodImpl.EntityId

    let methodImplId = findRtcbenchmarkMethodImplId dih "rtcbenchmark_measure_java_performance"
    let methodImpl = rtcdata.MethodImpls |> Seq.find (fun impl -> impl.MethodImplId = methodImplId)

    let optimisedAvr = methodImpl.AvrInstructions |> Seq.map AvrInstructionFromXml |> Seq.toList
    let unoptimisedAvrWithJvmIndex =
        methodImpl.JavaInstructions |> Seq.map (fun jvm -> jvm.UnoptimisedAvr.AvrInstructions |> Seq.map (fun avr -> (AvrInstructionFromXml avr, JvmInstructionFromXml jvm)))
                                    |> Seq.concat
                                    |> Seq.toList

    let matchedResult = matchOptUnopt optimisedAvr unoptimisedAvrWithJvmIndex
    let matchedResultWithCycles = addCycles (methodImpl.JavaInstructions |> Seq.map JvmInstructionFromXml |> Seq.toList) profilerdata matchedResult
    let stopwatchTimers = getTimersFromStdout (stdoutlog |> Seq.toList)
    let (cyclesPush, cyclesPop , cyclesMovw) = (countPushPopMovw matchedResultWithCycles)

    let groupFold keyFunc valueFunc foldFunc foldInitAcc x =
        x |> List.toSeq
          |> Seq.groupBy keyFunc
          |> Seq.map (fun (key, groupedResults) -> (key, groupedResults |> Seq.map valueFunc |> Seq.fold foldFunc foldInitAcc))
          |> Seq.toList

    let cyclesPerJvmOpcode =
        matchedResultWithCycles
            |> List.filter (fun r -> r.jvm.text <> "Method preamble")
            |> groupFold (fun r -> r.jvm.text.Split().First()) (fun r -> r.counters) (+) ExecCounters.empty
            |> List.sortBy (fun (opcode, _) -> (getCategoryForJvmOpcode opcode)+opcode)

    let nativeCInstructions = getNativeInstructionsFromObjdump disasm profilerdata

    let cyclesPerAvrOpcodeNativeC =
        nativeCInstructions
            |> List.map (fun (avr, cnt) -> (AVR.getOpcodeForInstruction avr.opcode avr.text, cnt))
            |> groupFold fst snd (+) ExecCounters.empty

    let cyclesPerAvrOpcodeAOTJava =
        matchedResultWithCycles
            |> List.map (fun r -> r.avr)
            |> List.concat
            |> List.filter (fun avr -> avr.opt.IsSome)
            |> List.map (fun avr -> (AVR.getOpcodeForInstruction avr.opt.Value.opcode avr.opt.Value.text, avr.counters))
            |> groupFold fst snd (+) ExecCounters.empty

    let groupOpcodesInCategories (allCategories : string list) (getCategory : ('a -> string)) (results : ('a * ExecCounters) list) =
        let categoriesPresent =
            results
                |> List.map (fun (opcode, cnt) -> (getCategory opcode, cnt))
                |> groupFold fst snd (+) ExecCounters.empty
        allCategories
            |> List.sort
            |> List.map (fun cat -> match categoriesPresent |> List.tryFind (fun (cat2, _) -> cat = cat2) with
                                    | Some (cat, cnt) -> (cat, cnt)
                                    | None -> (cat, ExecCounters.empty))

    let cyclesPerJvmOpcodeCategory = groupOpcodesInCategories getAllJvmOpcodeCategories getCategoryForJvmOpcode cyclesPerJvmOpcode
    let cyclesPerAvrOpcodeCategoryNativeC = groupOpcodesInCategories AVR.getAllOpcodeCategories AVR.opcodeCategory cyclesPerAvrOpcodeNativeC
    let cyclesPerAvrOpcodeCategoryAOTJava = groupOpcodesInCategories AVR.getAllOpcodeCategories AVR.opcodeCategory cyclesPerAvrOpcodeAOTJava

    {
        benchmark = benchmark;
        jvmInstructions = matchedResultWithCycles;
        nativeCInstructions = nativeCInstructions |> Seq.toList;
        executedCyclesAOT = cyclesPerJvmOpcodeCategory |> List.sumBy (fun (cat, cnt) -> cnt.cycles);
        executedCyclesC = cyclesPerAvrOpcodeCategoryNativeC |> List.sumBy (fun (cat, cnt) -> cnt.cycles);
        stopwatchCyclesJava = stopwatchTimers |> List.find (fun (t,c) -> t="Java") |> snd;
        stopwatchCyclesAOT = stopwatchTimers |> List.find (fun (t,c) -> t="AOT") |> snd;
        stopwatchCyclesC = stopwatchTimers |> List.find (fun (t,c) -> t="C") |> snd;
        cyclesPush = cyclesPush;
        cyclesPop = cyclesPop;
        cyclesMovw = cyclesMovw;
        cyclesPerJvmOpcode = cyclesPerJvmOpcode |> List.map (fun (opc, cnt) -> (getCategoryForJvmOpcode opc, opc, cnt));
        cyclesPerAvrOpcodeAOTJava = cyclesPerAvrOpcodeAOTJava |> List.map (fun (opc, cnt) -> (AVR.opcodeCategory opc, AVR.opcodeName opc, cnt));
        cyclesPerAvrOpcodeNativeC = cyclesPerAvrOpcodeNativeC |> List.map (fun (opc, cnt) -> (AVR.opcodeCategory opc, AVR.opcodeName opc, cnt));
        cyclesPerJvmOpcodeCategory = cyclesPerJvmOpcodeCategory;
        cyclesPerAvrOpcodeCategoryAOTJava = cyclesPerAvrOpcodeCategoryAOTJava;
        cyclesPerAvrOpcodeCategoryNativeC = cyclesPerAvrOpcodeCategoryNativeC;
    }

let resultsToString (results : Results) =
    let totalCyclesAOTJava = results.jvmInstructions |> List.sumBy (fun r -> (r.counters.cycles))
    let totalCyclesNativeC = results.nativeCInstructions |> List.sumBy (fun (inst,cnt) -> (cnt.cycles))
    let countersToString totalCycles (counters : ExecCounters) =
        String.Format("exe:{0,8} cyc:{1,10} {2:00.000}% avg: {3:00.00}",
                      counters.executions,
                      counters.cycles,
                      100.0 * float (counters.cycles) / float totalCycles,
                      counters.average)
    let resultJavaToString (result : ResultJava) =
        String.Format("{0,-60}{1}\r\n",
                      result.jvm.text,
                      countersToString totalCyclesAOTJava result.counters)
    let resultsAvrToString (avrResults : ResultAvr list) =
        let avrInstOption2Text = function
            | Some (x : AvrInstruction)
                -> String.Format("{0:10}: {1,-15}", x.address, x.text)
            | None -> "" in
        avrResults |> List.map (fun r -> String.Format("        {0,-15} -> {1,-36} {2,8} {3,14}\r\n",
                                                      r.unopt.text,
                                                      avrInstOption2Text r.opt,
                                                      r.counters.executions,
                                                      r.counters.cycles))
                   |> List.fold (+) ""
    let nativeCInstructionToString ((inst, counters) : AvrInstruction*ExecCounters) =
        String.Format("0x{0,6:X6}: {1}    {2}\r\n", inst.address, (countersToString totalCyclesNativeC counters), inst.text)
    let r1 = "--- COMPLETE LISTING\r\n"
             + (results.jvmInstructions
                    |> List.map (fun r -> (r |> resultJavaToString) + (r.avr |> resultsAvrToString))
                    |> List.fold (+) "")
    let r2 = "--- ONLY OPTIMISED AVR\r\n"
             + (results.jvmInstructions
                    |> List.map (fun r -> (r |> resultJavaToString) + (r.avr |> List.filter (fun avr -> avr.opt.IsSome) |> resultsAvrToString))
                    |> List.fold (+) "")
    let r3 = "--- ONLY JVM\r\n"
             + (results.jvmInstructions
                    |> List.map resultJavaToString
                    |> List.fold (+) "")
    let r4 = "--- NATIVE C AVR\r\n"
             + (results.nativeCInstructions
                    |> List.map nativeCInstructionToString
                    |> List.fold (+) "")

    let opcodeResultsToString totalCycles opcodeResults =
            opcodeResults
            |> List.map (fun (category, opcode, counters)
                             ->  String.Format("{0,-20}{1,-20} total {2}\r\n",
                                               category,
                                               opcode,
                                               (countersToString totalCycles counters)))
            |> List.fold (+) ""
    let categoryResultsToString totalCycles categoryResults =
            categoryResults
            |> List.map (fun (category, counters)
                             -> String.Format("{0,-40} total {1}\r\n",
                                              category,
                                              (countersToString totalCycles counters)))
            |> List.fold (+) ""

    let r5 = "--- SUMMED PER AVR OPCODE (NATIVE C)\r\n"
             + opcodeResultsToString totalCyclesNativeC results.cyclesPerAvrOpcodeNativeC
    let r6 = "--- SUMMED PER AVR OPCODE (Java)\r\n"
             + opcodeResultsToString totalCyclesAOTJava results.cyclesPerAvrOpcodeAOTJava
    let r7 = "--- SUMMED PER JVM OPCODE\r\n"
             + opcodeResultsToString totalCyclesAOTJava results.cyclesPerJvmOpcode
    let r8 = "--- SUMMED PER AVR CATEGORY (NATIVE C)\r\n"
             + categoryResultsToString totalCyclesNativeC results.cyclesPerAvrOpcodeCategoryNativeC
    let r9 = "--- SUMMED PER AVR CATEGORY (Java)\r\n"
             + categoryResultsToString totalCyclesAOTJava results.cyclesPerAvrOpcodeCategoryAOTJava
    let r10 = "--- SUMMED PER JVM CATEGORY\r\n"
             + categoryResultsToString totalCyclesAOTJava results.cyclesPerJvmOpcodeCategory
    let r11 = "--- TOTAL CYCLES SPENT IN COMPILED JVM CODE: " + totalCyclesAOTJava.ToString() + "\r\n    (doesn't include called functions)"

    let r12 = (String.Format ("--- TOTAL SPENT ON PUSH: {0}\r\n\
                              --- TOTAL SPENT ON POP:  {1}\r\n\
                              --- TOTAL SPENT ON MOVW: {2}\r\n\
                              --- COMBINED:            {3}",
                              (countersToString totalCyclesAOTJava results.cyclesPush),
                              (countersToString totalCyclesAOTJava results.cyclesPop),
                              (countersToString totalCyclesAOTJava results.cyclesMovw),
                              (countersToString totalCyclesAOTJava (results.cyclesPush + results.cyclesPop + results.cyclesMovw))))

    "------------------ " + results.benchmark + " ------------------\r\n\r\n" + r12 + "\r\n\r\n" + r11 + "\r\n\r\n" + r10 + "\r\n\r\n" + r9 + "\r\n\r\n" + r8 + "\r\n\r\n" + r7 + "\r\n\r\n" + r6 + "\r\n\r\n" + r5 + "\r\n\r\n" + r4 + "\r\n\r\n" + r3 + "\r\n\r\n" + r2 + "\r\n\r\n" + r1

let main(args : string[]) =
    let benchmark = (Array.get args 1).[3..]
    let builddir = (Array.get args 2)
    let outputfilename = (Array.get args 3)

    let dih = DarjeelingInfusionHeaderXml.Load(String.Format("{0}/infusion-bm_{1}/bm_{1}.dih", builddir, benchmark))
    let rtcdata = RtcdataXml.Load(String.Format("{0}/rtcdata.xml", builddir))
    let profilerdata = ProfilerdataXml.Load(String.Format("{0}/profilerdata.xml", builddir)).Instructions |> Seq.toList
    let stdoutlog = System.IO.File.ReadLines(String.Format("{0}/stdoutlog.txt", builddir))
    let disasm = System.IO.File.ReadLines(String.Format("{0}/darjeeling.S", builddir)) |> Seq.toList
    let results = processTrace benchmark dih rtcdata profilerdata stdoutlog disasm

    let txtFilename = outputfilename + ".txt"
    let xmlFilename = outputfilename + ".xml"

    File.WriteAllText (txtFilename, (resultsToString results))
    Console.Error.WriteLine ("Wrote output to " + txtFilename)

    let xmlSerializer = FsPickler.CreateXmlSerializer(indent = true)
    File.WriteAllText (xmlFilename, (xmlSerializer.PickleToString results))
    Console.Error.WriteLine ("Wrote output to " + xmlFilename)
    1

main(fsi.CommandLineArgs)
// main([|
//         "sortO"
//         "/Users/niels/src/rtc/src/build/avrora"
//         "/Users/niels/src/rtc/src/config/avrora/tmpoutput"
//      |])


















