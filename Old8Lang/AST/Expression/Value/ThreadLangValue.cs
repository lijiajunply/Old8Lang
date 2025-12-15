using System.Threading;
using System.Reflection.Emit;
using Old8Lang.LangParser;
using Old8Lang.Error;
using Old8Lang.Compiler;
using Old8Lang.AST.Expression.Intermediates;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 线程值类型，用于表示和管理线程
/// </summary>
public class ThreadLangValue : LangValueType
{
    /// <summary>
    /// 线程对象
    /// </summary>
    private readonly Thread _thread;
    
    /// <summary>
    /// 线程执行结果
    /// </summary>
    private object? _result;
    
    /// <summary>
    /// 线程执行是否完成
    /// </summary>
    private bool _isCompleted;
    
    /// <summary>
    /// 线程执行过程中发生的异常
    /// </summary>
    private Exception? _exception;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="threadStart">线程入口点</param>
    /// <param name="position">源代码位置</param>
    public ThreadLangValue(ThreadStart threadStart, SourcePosition position = default) : base(position)
    {
        _thread = new Thread(threadStart);
        _thread.Start();
    }
    
    /// <summary>
    /// 构造函数，带参数
    /// </summary>
    /// <param name="parameterizedThreadStart">带参数的线程入口点</param>
    /// <param name="parameter">线程参数</param>
    /// <param name="position">源代码位置</param>
    public ThreadLangValue(ParameterizedThreadStart parameterizedThreadStart, object? parameter, SourcePosition position = default) : base(position)
    {
        _thread = new Thread(parameterizedThreadStart);
        _thread.Start(parameter);
    }
    
    /// <summary>
    /// 等待线程完成
    /// </summary>
    /// <returns>线程执行结果</returns>
    public LangValueType Join()
    {
        _thread.Join();
        
        if (_exception != null)
        {
            throw new InvalidOperationError(this, "线程执行异常: " + _exception.Message);
        }
        
        return ObjToValue(_result!);
    }
    
    /// <summary>
    /// 设置线程执行结果
    /// </summary>
    /// <param name="result">执行结果</param>
    public void SetResult(object result)
    {
        _result = result;
        _isCompleted = true;
    }
    
    /// <summary>
    /// 设置线程执行异常
    /// </summary>
    /// <param name="exception">异常对象</param>
    public void SetException(Exception exception)
    {
        _exception = exception;
        _isCompleted = true;
    }
    
    /// <summary>
    /// 获取线程状态
    /// </summary>
    public ThreadState State => _thread.ThreadState;
    
    /// <summary>
    /// 检查线程是否已完成
    /// </summary>
    public bool IsCompleted => _isCompleted;
    
    /// <summary>
    /// 将对象转换为语言值类型
    /// </summary>
    /// <param name="obj">要转换的对象</param>
    /// <returns>转换后的语言值类型</returns>
    private new static LangValueType ObjToValue(object obj)
    {
        return obj switch
        {
            int i => new IntLangValue(i),
            double d => new DoubleLangValue(d),
            string s => new StringLangValue(s),
            bool b => new BoolLangValue(b),
            char c => new CharLangValue(c),
            null => new NullLangValue(),
            _ => new VoidLangValue()  // 对于未知类型，返回 VoidLangValue
        };
    }
    
    /// <summary>
    /// 运行方法，用于支持线程对象的方法调用
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="args">方法参数</param>
    /// <returns>方法调用结果</returns>
    public LangValueType Run()
    {
        // 处理 Join 方法调用
        return Join();
    }
    
    public override Type OutputType(LocalManager local)
    {
        return typeof(ThreadLangValue);
    }
    
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 加载当前线程对象到 IL 栈
        ilGenerator.Emit(OpCodes.Ldarg_0);
    }
    
    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 声明局部变量
        var localVar = ilGenerator.DeclareLocal(typeof(ThreadLangValue));
        // 添加到局部变量管理器
        local.AddLocalVar(idName, localVar);
        // 设置线程对象到 IL 变量
        ilGenerator.Emit(OpCodes.Stloc, localVar);
    }
    
    public override object GetValue()
    {
        return this;
    }
}