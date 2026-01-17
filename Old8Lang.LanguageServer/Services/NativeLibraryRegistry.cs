namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 原生库函数注册表
/// 提供 Old8LangLib 中所有可用函数的元数据，用于代码补全
/// </summary>
public static class NativeLibraryRegistry
{
    /// <summary>
    /// 获取所有原生库函数名称
    /// </summary>
    public static IEnumerable<string> GetAllFunctionNames()
    {
        return
        [
            // Math functions (MathLib)
            "Sqrt", "Abs", "Max", "Min", "Pow", "Log", "Log10", "LogBase",
            "Sin", "Cos", "Tan", "Asin", "Acos", "Atan", "Atan2",
            "Sinh", "Cosh", "Tanh",
            "Ceiling", "Floor", "Round", "Truncate",
            "Exp", "Sign",

            // String functions (StringLib - if exists, or built-in)
            "Concat", "Substring", "Contains", "Replace",
            "ToUpper", "ToLower", "Trim", "TrimStart", "TrimEnd",
            "Split", "Join", "StartsWith", "EndsWith",
            "IndexOf", "LastIndexOf",

            // Conversion functions
            "ToInt", "ToDouble", "ToBool", "ToChar",
            "ToStr", "ToString",

            // Collection functions
            "Add", "Remove", "Count", "Clear", "IsEmpty",
            "Contains", "IndexOf", "Insert", "RemoveAt",
            "First", "Last", "Take", "Skip", "Reverse",
            "Sort", "Filter", "Map", "Reduce",

            // Type functions
            "TypeOf", "IsType", "GetType",

            // Async functions
            "Delay", "WhenAll", "WhenAny", "Await",

            // File functions (FileLib)
            "OpenFile", "ReadFile", "WriteFile", "CloseFile",
            "DeleteFile", "FileExists", "CopyFile", "MoveFile",
            "GetFileSize", "GetFileInfo",
            "FileReadLines", "FileWriteLines", "FileReadAllText", "FileWriteAllText",
            "FileReadAllBytes", "FileWriteAllBytes",
            "FileAppendText", "FileAppendLines",

            // Directory functions
            "CreateDirectory", "DeleteDirectory", "DirectoryExists",
            "GetFiles", "GetDirectories", "GetFileSystemEntries",

            // OS functions (OS)
            "OsInfo", "Platform", "Environ", "GetEnv", "SetEnv",
            "GetCurrentDirectory", "SetCurrentDirectory",
            "GetUserName", "GetMachineName", "GetOSVersion",
            "ExecuteCommand", "StartProcess", "KillProcess",

            // Terminal functions (Terminal, ColorfulTerminal)
            "Clear", "Write", "WriteLine", "Read", "ReadKey",
            "SetCursorPosition", "GetCursorPosition",
            "SetForegroundColor", "SetBackgroundColor", "ResetColor",
            "ClearLine", "MoveCursorUp", "MoveCursorDown",

            // JSON functions (JsonLib)
            "Parse", "Stringify", "JsonType", "ToJson", "FromJson",
            "JsonParse", "JsonStringify", "JsonMinify", "JsonPrettify",
            "JsonValidate", "JsonPath",

            // Time functions (Time)
            "Now", "Ticks", "UnixTime", "Format",
            "AddDays", "AddHours", "AddMinutes", "AddSeconds",
            "GetYear", "GetMonth", "GetDay", "GetHour", "GetMinute", "GetSecond",
            "ParseTime", "FormatTime", "GetTimeNow",
            "DateTimeDiff", "DateTimeCompare",

            // Random functions (MathLib or RandomLib)
            "Random", "RandomInt", "RandomDouble", "RandomBool",
            "RandomString", "RandomBytes", "SetRandomSeed",

            // Vector/Matrix functions (if exists)
            "Create", "Dot", "Cross", "Magnitude", "Normalize",
            "VectorAdd", "VectorSubtract", "VectorMultiply", "VectorDivide",
            "VectorAbs", "VectorSqrt", "VectorMin", "VectorMax",

            // Cryptography functions (CryptoLib)
            "Md5", "Sha1", "Sha256", "Sha512",
            "AesEncrypt", "AesDecrypt",
            "RsaEncrypt", "RsaDecrypt", "RsaSign", "RsaVerify",
            "Base64Encode", "Base64Decode",
            "HmacSha256", "HmacSha512",
            "Sha256Hash", "HmacSha256Hash",

            // Encoding functions
            "UrlEncode", "UrlDecode",
            "HtmlEncode", "HtmlDecode",

            // Network functions (if in NetLib - but these might require import)
            "HttpGet", "HttpPost", "HttpPut", "HttpDelete",
            "WebSocketConnect", "WebSocketSend", "WebSocketReceive", "WebSocketClose",

            // Database functions (if exists - might require import)
            "DbConnect", "DbQuery", "DbExecute", "DbClose",

            // Assert/Test functions (AssertLib)
            "AssertEqual", "AssertNotEqual", "AssertTrue", "AssertFalse",
            "AssertNull", "AssertNotNull", "AssertThrows",

            // Collection/LINQ-like functions
            "Where", "Select", "OrderBy", "OrderByDescending",
            "GroupBy", "Distinct", "Union", "Intersect", "Except",
            "Any", "All", "Sum", "Average", "Min", "Max",

            // Template Engine (TemplateEngine)
            "RenderTemplate", "CompileTemplate",

            // Image functions (ImageLib)
            "LoadImage", "SaveImage", "ResizeImage", "CropImage",
            "RotateImage", "FlipImage"
        ];
    }
}
