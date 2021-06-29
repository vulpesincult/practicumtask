

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;





namespace ConsoleApplicationBase
{
    class Program
    {
        const string _commandsNamespace = "ConsoleApplicationBase.Commands";
        static Dictionary<string, Dictionary<string, IEnumerable<ParameterInfo>>> _commandLibraries;

        static void Main(string[] args)
        {
            _commandLibraries = new Dictionary<string, Dictionary<string, IEnumerable<ParameterInfo>>>(StringComparer.InvariantCultureIgnoreCase);

            // use reflection to load all of the classes in the Commands namespace
            IEnumerable<Type> query =   from type in Assembly.GetExecutingAssembly().GetTypes()
                                        where type.IsClass && type.Namespace == _commandsNamespace
                                        select type;

            List<Type> commandClasses = query.ToList();

            foreach (Type commandClass in commandClasses)
            {
                MethodInfo[] methods = commandClass.GetMethods(BindingFlags.Static | BindingFlags.Public);
                var methodDictionary = new Dictionary<string, IEnumerable<ParameterInfo>>(StringComparer.InvariantCultureIgnoreCase);
                foreach (MethodInfo method in methods)
                {
                    methodDictionary.Add(method.Name, method.GetParameters());
                }

                _commandLibraries.Add(commandClass.Name, methodDictionary);
            }

            Run();
        }


        static void Run()
        {
            PrintHelpMessage();

            while (true)
            {
                // get input. parse to ConsoleCommand object. execute. print the result

                string consoleInput = ReadFromConsole();

                if (string.IsNullOrWhiteSpace(consoleInput))
                {
                    continue;
                }

                if (consoleInput.ToLower() == "help")
                {
                    PrintHelpMessage();
                }

                try
                {
                    ConsoleCommand cmd = new ConsoleCommand(consoleInput);
                    WriteToConsole(Execute(cmd));
                }
                catch (Exception ex)
                {
                    WriteToConsole(ex.Message);
                }
            }
        }


        static string Execute(ConsoleCommand command)
        {
            // check whether the command exist and enough parameters were passed

            if (!_commandLibraries.ContainsKey(command.LibraryClassName))
            {
                return string.Format("Unrecognized library \'{0}\'.", command.LibraryClassName);
            }
            var nameLibraryPair = _commandLibraries.Single(pair => pair.Key.ToLower() == command.LibraryClassName.ToLower());
            command.LibraryClassName = nameLibraryPair.Key;

            Dictionary<string, IEnumerable<ParameterInfo>> methodDictionary = _commandLibraries[command.LibraryClassName];
            if (!methodDictionary.ContainsKey(command.Name))
            {
                return string.Format("Unrecognized command \'{0}.{1}\'.", command.LibraryClassName, command.Name);
            }
            var nameMethodPair = methodDictionary.Single(pair => pair.Key.ToLower() == command.Name.ToLower());
            command.Name = nameMethodPair.Key;

            List<object> methodParameterValueList = new List<object>();
            IEnumerable<ParameterInfo> paramInfoList = methodDictionary[command.Name].ToList();
            
            int requiredParamsCount = paramInfoList.Where(param => !param.IsOptional).Count();
            int optionalParamsCount = paramInfoList.Where(param => param.IsOptional).Count();
            int providedParamsCount = command.Arguments.Count();

            if (requiredParamsCount > providedParamsCount)
            {
                return string.Format("Missing required argument. {0} required, {1} optional, {2} provided", requiredParamsCount, optionalParamsCount, providedParamsCount);
            }

            if (paramInfoList.Count() > 0)
            {
                // set parameters

                foreach (ParameterInfo param in paramInfoList)
                {
                    if (param.HasDefaultValue)
                    {
                        methodParameterValueList.Add(param.DefaultValue);
                    }
                    else
                    {
                        methodParameterValueList.Add(null);
                    }
                }
                
                for (int i = 0; i < command.Arguments.Count(); i++)
                {
                    Type requiredType = paramInfoList.ElementAt(i).ParameterType;
                    try
                    {
                        object value = CoerceArgument(requiredType, command.Arguments.ElementAt(i));
                        methodParameterValueList.RemoveAt(i);
                        methodParameterValueList.Insert(i, value);
                    }
                    catch (ArgumentException)
                    {
                        string message = string.Format("The value passed for argument '{0}' cannot be parsed to type '{1}'", paramInfoList.ElementAt(i).Name, requiredType.Name);
                        throw new ArgumentException(message);
                    }
                }
            }

            // invoke the method using reflection

            Assembly current = typeof(Program).Assembly;
            
            Type commandLibaryClass = current.GetType(_commandsNamespace + "." + command.LibraryClassName);

            object[] inputArgs = null;
            if (methodParameterValueList.Count > 0)
            {
                inputArgs = methodParameterValueList.ToArray();
            }

            try
            {
                var result = commandLibaryClass.InvokeMember(command.Name, BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, inputArgs);

                return result.ToString();
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
        
        static void TryOrThrow(Func<bool> func, string exceptionMessage)
        {
            if (!func())
            {
                throw new ArgumentException(exceptionMessage);
            }
        }

        static object CoerceArgument(Type requiredType, string inputValue)
        {
            TypeCode requiredTypeCode = Type.GetTypeCode(requiredType);
            string exceptionMessage = string.Format("Cannnot coerce the input argument {0} to required type {1}", inputValue, requiredType.Name);

            object result = null;
            switch (requiredTypeCode)
            {
                case TypeCode.String:
                    result = inputValue;
                    break;
                case TypeCode.Int16:
                    Int16 int16Number;
                    TryOrThrow(() => Int16.TryParse(inputValue, out int16Number), exceptionMessage);
                    break;
                case TypeCode.Int32:
                    Int32 int32Number;
                    TryOrThrow(() => Int32.TryParse(inputValue, out int32Number), exceptionMessage);
                    break;
                case TypeCode.Int64:
                    Int64 int64Number;
                    TryOrThrow(() => Int64.TryParse(inputValue, out int64Number), exceptionMessage);
                    break;
                case TypeCode.Boolean:
                    Boolean boolean;
                    TryOrThrow(() => Boolean.TryParse(inputValue, out boolean), exceptionMessage);
                    break;
                case TypeCode.Byte:
                    Byte byteNumber;
                    TryOrThrow(() => Byte.TryParse(inputValue, out byteNumber), exceptionMessage);
                    break;
                case TypeCode.Char:
                    Char character;
                    TryOrThrow(() => Char.TryParse(inputValue, out character), exceptionMessage);
                    break;
                case TypeCode.DateTime:
                    DateTime dateTime;
                    TryOrThrow(() => DateTime.TryParse(inputValue, out dateTime), exceptionMessage);
                    break;
                case TypeCode.Decimal:
                    Decimal decimalNumber;
                    TryOrThrow(() => Decimal.TryParse(inputValue, out decimalNumber), exceptionMessage);
                    break;
                case TypeCode.Double:
                    Double doubleNumber;
                    TryOrThrow(() => Double.TryParse(inputValue, out doubleNumber), exceptionMessage);
                    break;
                case TypeCode.Single:
                    Single singleNumber;
                    TryOrThrow(() => Single.TryParse(inputValue, out singleNumber), exceptionMessage);
                    break;
                case TypeCode.UInt16:
                    UInt16 uint16Number;
                    TryOrThrow(() => UInt16.TryParse(inputValue, out uint16Number), exceptionMessage);
                    break;
                case TypeCode.UInt32:
                    UInt32 uint32Number;
                    TryOrThrow(() => UInt32.TryParse(inputValue, out uint32Number), exceptionMessage);
                    break;
                case TypeCode.UInt64:
                    UInt64 uint64Number;
                    TryOrThrow(() => UInt64.TryParse(inputValue, out uint64Number), exceptionMessage);
                    break;
                default:
                    throw new ArgumentException(exceptionMessage);
            }
            
            return result;
        }

        public static void PrintHelpMessage()
        {
            foreach (var nameCommandClassPair in _commandLibraries)
            {
                var commandClass = nameCommandClassPair.Value;
                foreach (var method in commandClass)
                {
                    StringBuilder signature = new StringBuilder();
                    if (nameCommandClassPair.Key == "DefaultCommands")
                    {
                        signature.Append(string.Format("command: {0}", method.Key));
                    }
                    else
                    {
                        signature.Append(string.Format("command: {0}.{1}", nameCommandClassPair.Key, method.Key));
                    }

                    foreach (var param in method.Value)
                    {
                        signature.Append(string.Format(" <{0}>", param.Name));
                    }
                    signature.Append("\n");
                    foreach (var param in method.Value)
                    {
                        signature.Append(string.Format("parameter <{0}>: type = {1}; {2}\n", param.Name, param.ParameterType.ToString(), (param.IsOptional)? "optional": ""));
                    }
                    Console.WriteLine(signature);
                }
            }
        }

        public static void WriteToConsole(string message = "")
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine(message);
            }
        }
        
        public static string ReadFromConsole(string promptMessage = "")
        {
            Console.Write("console> " + promptMessage);
            return Console.ReadLine();
        }
    }
}