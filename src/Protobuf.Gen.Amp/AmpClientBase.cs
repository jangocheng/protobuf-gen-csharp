using System;
using System.IO;
using System.Text;
using Google.Protobuf.Compiler;
using Google.Protobuf.Reflection;
using Protobuf.Gen.Core;

namespace Protobuf.Gen.Amp
{
    public class AmpClientBase : AmpPluginBase
    {
        protected override void GenerateByEachFile(FileDescriptorProto protofile, CodeGeneratorResponse response)
        {
            bool genericClient;
            protofile.Options.CustomOptions.TryGetBool(DotBPEOptions.DISABLE_GENERIC_SERVICES_CLIENT, out genericClient);
            if (genericClient)
            {
                return;
            }
            if (protofile.Service == null || protofile.Service.Count <= 0) return;
            //生成文件头
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Generated by the protocol buffer compiler.  DO NOT EDIT!");
            sb.AppendLine($"// source: {protofile.Name}");
            //还可以生成注释

            sb.AppendLine("#region Designer generated code");
            sb.AppendLine("");
            sb.AppendLine("using System; ");
            sb.AppendLine("using System.Threading.Tasks; ");
            sb.AppendLine("using DotBPE.Rpc; ");
            sb.AppendLine("using DotBPE.Protocol.Amp; ");
            sb.AppendLine("using DotBPE.Rpc.Exceptions; ");
            sb.AppendLine("using Google.Protobuf; ");
            sb.AppendLine("");

            string ns = Utils.GetFileNamespace(protofile);
            sb.AppendLine("namespace " + ns + " {");
            //生成代码

            foreach (ServiceDescriptorProto t in protofile.Service)
            {
                t.Options.CustomOptions.TryGetBool(DotBPEOptions.DISABLE_GENERIC_SERVICES_CLIENT, out genericClient);
                if (genericClient)
                {
                    continue;
                }
                sb.AppendLine("");
                sb.AppendLine("//start for class "+t.Name+"Client");
                GenerateServiceForClient(t, sb);
                sb.AppendLine("//end for class "+t.Name+"Client");
            }
            sb.AppendLine("}");
            sb.AppendLine("#endregion");

            //生成文件
            var nfile = new CodeGeneratorResponse.Types.File
            {
                Name = Utils.GetFileName(protofile.Name) + "Client.cs",
                Content = sb.ToString()
            };
            response.File.Add(nfile);
        }

        private static void GenerateServiceForClient(ServiceDescriptorProto service, StringBuilder sb)
        {
            int serviceId;
            bool hasServiceId = service.Options.CustomOptions.TryGetInt32(DotBPEOptions.SERVICE_ID, out serviceId);
            if (!hasServiceId || serviceId <= 0)
            {
                throw new Exception("Service=" + service.Name + " ServiceId NOT_FOUND");
            }
            if (serviceId >= ushort.MaxValue)
            {
                throw new Exception("Service=" + service.Name + "ServiceId too large");
            }

            sb.AppendFormat("public sealed class {0}Client : AmpInvokeClient \n",service.Name);
            sb.AppendLine("{");
            //构造函数
            sb.AppendLine($"public {service.Name}Client(IRpcClient<AmpMessage> client) : base(client)");
            sb.AppendLine("{");
            sb.AppendLine("}");

            //循环方法
            foreach (var method in service.Method)
            {
                int msgId ;
                bool hasMsgId= method.Options.CustomOptions.TryGetInt32(DotBPEOptions.MESSAGE_ID,out msgId);
                if (!hasMsgId || msgId <= 0)
                {
                    throw new Exception("Service" + service.Name + "." + method.Name + " ' MessageId NOT_FINDOUT ");
                }
                if (msgId >= ushort.MaxValue)
                {
                    throw new Exception("Service" + service.Name + "." + method.Name + " is too large");
                }
                //异步方法
                string outType = Utils.GetTypeName(method.OutputType);
                string inType = Utils.GetTypeName(method.InputType);

                sb.AppendLine($"public async Task<{outType}> {method.Name}Async({inType} request,int timeOut=3000)");
                sb.AppendLine("{");
                sb.AppendLine($"AmpMessage message = AmpMessage.CreateRequestMessage({serviceId}, {msgId});");
                sb.AppendLine("message.Data = request.ToByteArray();");
                sb.AppendLine("var response = await base.CallInvoker.AsyncCall(message,timeOut);");
                sb.AppendLine("if (response == null)");
                sb.AppendLine("{");
                sb.AppendLine("throw new RpcException(\"error,response is null !\");");
                sb.AppendLine("}");
                sb.AppendLine($"return {outType}.Parser.ParseFrom(response.Data);");
                sb.AppendLine("}");

                sb.AppendLine();
                sb.AppendLine("//同步方法");
                sb.AppendLine($"public {outType} {method.Name}({inType} request)");
                sb.AppendLine("{");
                sb.AppendLine($"AmpMessage message = AmpMessage.CreateRequestMessage({serviceId}, {msgId});");
                sb.AppendLine("message.Data = request.ToByteArray();");
                sb.AppendLine("var response =  base.CallInvoker.BlockingCall(message);");
                sb.AppendLine("if (response == null)");
                sb.AppendLine("{");
                sb.AppendLine("throw new RpcException(\"error,response is null !\");");
                sb.AppendLine("}");
                sb.AppendLine($"return {outType}.Parser.ParseFrom(response.Data);");
                sb.AppendLine("}");
            //循环方法end
            }
            sb.AppendLine("}");
            //类结束
        }
    }
}
