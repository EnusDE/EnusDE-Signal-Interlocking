using System;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Reflection.Emit;

string managed = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (context, name) => {
    string path = Path.Combine(managed, name.Name + ".dll");
    if(name.Name=="EnusDE.Runenwacht")path=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","runenwacht","EnusDE.Runenwacht.dll"));
    return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(managed, args[1]));
if(args.Length>3&&args[2]=="worldsigns"){AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));Assembly.GetExecutingAssembly().GetType("WorldSignTextureTests").GetMethod("Run").Invoke(null,null);return;}
if(args.Length>3&&args[2]=="flank"){AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));FlankTests.Run();return;}
if(args.Length>3&&args[2]=="transfer"){AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));TransferTests.Run();return;}
if(args.Length>4&&args[2]=="signature"){foreach(var method in assembly.GetType(args[3],true).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static).Where(m=>m.Name==args[4])){System.Console.WriteLine(method.Name+" public="+method.IsPublic+" static="+method.IsStatic);foreach(var p in method.GetParameters())System.Console.WriteLine(p.Name+": "+p.ParameterType.FullName);}return;}
if(args.Length>3&&args[2]=="mentions") {
    Type[] all;try{all=assembly.GetTypes();}catch(ReflectionTypeLoadException e){all=e.Types.Where(t=>t!=null).ToArray();}
    var ops=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);
    foreach(var type in all)foreach(var method in type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly))try {
        var il=method.GetMethodBody()?.GetILAsByteArray();if(il==null)continue;
        for(int i=0;i<il.Length;) {
            ushort code=il[i++];if(code==0xfe)code=(ushort)(0xfe00|il[i++]);var op=ops[code];int size;
            switch(op.OperandType){case OperandType.InlineNone:size=0;break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;case OperandType.InlineVar:size=2;break;case OperandType.InlineI8:case OperandType.InlineR:size=8;break;case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;default:size=4;break;}
            if(op.OperandType==OperandType.InlineMethod||op.OperandType==OperandType.InlineField||op.OperandType==OperandType.InlineType)try{var member=method.Module.ResolveMember(BitConverter.ToInt32(il,i));if((member.DeclaringType+" "+member).Contains(args[3])){textMethod(method);break;}}catch{}
            i+=size;
        }
    }catch{}
    return;
}
if(args.Length>4&&args[2]=="il") {
    var target=assembly.GetType(args[3],true);
    var methodFlags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance|BindingFlags.DeclaredOnly;
    var methods=args[4]==".ctor"?target.GetConstructors(methodFlags).Cast<MethodBase>():target.GetMethods(methodFlags).Where(m=>m.Name==args[4]).Cast<MethodBase>();
    var table=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(o=>(ushort)o.Value);
    foreach(var method in methods) {
        textMethod(method);
        var il=method.GetMethodBody()?.GetILAsByteArray();if(il==null)continue;
        for(int i=0;i<il.Length;) {
            int offset=i;ushort code=il[i++];if(code==0xfe)code=(ushort)(0xfe00|il[i++]);var op=table[code];
            string operand="";int size=0;
            switch(op.OperandType){case OperandType.InlineNone:break;case OperandType.ShortInlineBrTarget:case OperandType.ShortInlineI:case OperandType.ShortInlineVar:size=1;break;case OperandType.InlineVar:size=2;break;case OperandType.InlineI8:case OperandType.InlineR:size=8;break;case OperandType.InlineSwitch:size=4+4*BitConverter.ToInt32(il,i);break;default:size=4;break;}
            if(op.OperandType==OperandType.InlineMethod||op.OperandType==OperandType.InlineField||op.OperandType==OperandType.InlineType||op.OperandType==OperandType.InlineTok)try{var member=method.Module.ResolveMember(BitConverter.ToInt32(il,i));operand=member.DeclaringType+" "+member;}catch{}
            if(op.OperandType==OperandType.InlineString)operand=method.Module.ResolveString(BitConverter.ToInt32(il,i));
            if(operand==""&&size==4&&op.OperandType==OperandType.InlineI)operand=BitConverter.ToInt32(il,i).ToString();
            if(operand==""&&size==1&&op.OperandType==OperandType.ShortInlineI)operand=((sbyte)il[i]).ToString();
            if(operand==""&&op.OperandType==OperandType.ShortInlineR)operand=BitConverter.ToSingle(il,i).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if(operand==""&&op.OperandType==OperandType.InlineR)operand=BitConverter.ToDouble(il,i).ToString(System.Globalization.CultureInfo.InvariantCulture);
            System.Console.WriteLine(offset+" "+op+" "+operand);
            i+=size;
        }
    }
    return;
}
static void textMethod(MethodBase method){System.Console.WriteLine(method.DeclaringType.FullName+"::"+method);}
if (args.Length > 4 && args[2] == "analyze") { DiagnosticsReport.Run(args[3],args[4]); return; }
if (args.Length > 3 && args[2] == "bridge") {
    AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));
    Assembly.GetExecutingAssembly().GetType("BridgeTests").GetMethod("Run").Invoke(null,null); return;
}
if (args.Length > 3 && args[2] == "native") {
    AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));
    Assembly.GetExecutingAssembly().GetType("NativeTests").GetMethod("Run").Invoke(null,new object[]{args[3]}); return;
}
if(args.Length>4&&args[2]=="network"){AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(Path.GetFullPath(args[3]),"build","recorder","EnusDE.Signaltechnik.dll"));Assembly.GetExecutingAssembly().GetType("NetworkTests").GetMethod("Run").Invoke(null,new object[]{args[4]});return;}
if (args.Length > 3 && args[2] == "test") {
    try { CommandTests.Run(Path.GetFullPath(args[3])); }
    catch(Exception error) { System.Console.Error.WriteLine(error); Environment.Exit(1); }
    return;
}
if (args.Length > 3 && args[2] == "memory") {
    try { BehaviorTests.Run(Path.GetFullPath(args[3]),true); }
    catch(Exception error) { System.Console.Error.WriteLine(error); Environment.Exit(1); }
    return;
}
if (args.Length > 3 && args[2] == "compile") {
    var interpreter = Activator.CreateInstance(assembly.GetType("Relay.Interpreter", true));
    try {
        interpreter.GetType().GetMethod("Compile", new[] { typeof(string), typeof(string) })
            .Invoke(interpreter, new object[] { File.ReadAllText(args[3]), Path.GetFullPath(args[3]) });
        System.Console.WriteLine("PASS: actual installed Relay compiler accepted " + Path.GetFileName(args[3]));
    } catch (TargetInvocationException error) { System.Console.Error.WriteLine(error.InnerException); Environment.Exit(1); }
    return;
}
if (args.Length > 3 && args[2] == "type") {
    var target = assembly.GetType(args[3], true);
    System.Console.WriteLine("TYPE " + target.FullName);
    System.Console.WriteLine("BASE " + target.BaseType);
    foreach (var member in target.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | (args.Length>4 && args[4]=="all"?BindingFlags.NonPublic:0)))
        System.Console.WriteLine(member is FieldInfo field&&field.IsLiteral?member+" = "+field.GetRawConstantValue():member);
} else {
    string pattern = args.Length > 2 ? args[2] : "Compiler|Interpreter|NativeModule|NativeFunction|VM|Value|Module";
    Type[] types;
    try { types = assembly.GetTypes(); }
    catch (ReflectionTypeLoadException error) {
        types = error.Types.Where(t => t != null).ToArray();
        foreach (var problem in error.LoaderExceptions.Take(3)) System.Console.Error.WriteLine(problem.Message);
    }
    foreach (var type in types.Where(t => Regex.IsMatch(t.FullName, pattern) && !Regex.IsMatch(t.FullName, "<|Display|Enumerator")))
        System.Console.WriteLine(type.FullName);
}
