# Lexer

在本文中，我们将使用csly来进行开发：[csly](https://github.com/b3b00/csly/wiki/getting-started)

首先我们需要定义一个枚举，就叫OldTokenGeneric，表示是使用了[通用语法器](https://github.com/b3b00/csly/wiki/GenericLexer)。

在csly中，有下列几种词素：

- keyword 关键词：具有特殊含义的标识符
- string 带有 “” 的标识符
- Identifier 标识符
- Int ： 整数
- Double ： 浮点数（小数分隔符为点 '.'）
- Sugar：通用词法，除了使用前导字母字符外，没有特殊约束
- UpTo：匹配所有模式，直到找到某些模式

现在我们可以简单的写出该类：

```csharp
public enum OldTokenGeneric
{
    #region keywords 0 -> 19

        [Lexeme(GenericToken.KeyWord, "IF")] [Lexeme(GenericToken.KeyWord, "if")]
        IF = 1,

        [Lexeme(GenericToken.KeyWord, "ELIF")] [Lexeme(GenericToken.KeyWord, "elif")]
        ELIF = 2,

        [Lexeme(GenericToken.KeyWord, "ELSE")] [Lexeme(GenericToken.KeyWord, "else")]
        ELSE = 3,

        [Lexeme(GenericToken.KeyWord, "WHILE")] [Lexeme(GenericToken.KeyWord, "while")]
        WHILE = 4,

        [Lexeme(GenericToken.KeyWord, "FOR")] [Lexeme(GenericToken.KeyWord, "for")]
        FOR = 5,
    
        [Lexeme(GenericToken.KeyWord, "TRUE")] [Lexeme(GenericToken.KeyWord, "true")]
        TRUE = 6,

        [Lexeme(GenericToken.KeyWord, "FALSE")] [Lexeme(GenericToken.KeyWord, "false")]
        FALSE = 7,

        [Lexeme(GenericToken.KeyWord, "NOT")] [Lexeme(GenericToken.KeyWord, "not")]
        NOT = 8,

        [Lexeme(GenericToken.KeyWord, "AND")] [Lexeme(GenericToken.KeyWord, "and")]
        AND = 9,
    
        [Lexeme(GenericToken.KeyWord, "OR")] [Lexeme(GenericToken.KeyWord, "or")]
        OR = 10,
    
        [Lexeme(GenericToken.KeyWord,"XOR")][Lexeme(GenericToken.KeyWord,"xor")]
        XOR = 11,
    
        [Lexeme(GenericToken.KeyWord,"CLASS")][Lexeme(GenericToken.KeyWord,"class")]
        CLASS = 14,
    
        [Lexeme(GenericToken.KeyWord,"FUNC")][Lexeme(GenericToken.KeyWord,"func")]
        FUNC = 15,
    
        [Lexeme(GenericToken.KeyWord,"RETURN")][Lexeme(GenericToken.KeyWord,"return")]
        RETURN = 16,
    
        [Lexeme(GenericToken.KeyWord,"IMPORT")][Lexeme(GenericToken.KeyWord,"import")]
        IMPORT = 17,

        #endregion

        #region literals 20 -> 29
        [Lexeme(GenericToken.Identifier)] IDENTFIER = 20,
        [Lexeme(GenericToken.String)] STRING = 21,
        [Lexeme(GenericToken.Int)] INT = 22,
        [Lexeme(GenericToken.Double)] DOUBLE = 23,
        [Lexeme(GenericToken.Char)] CHAR = 24,
        #endregion

        #region operators 30 -> 49
    
        [Sugar("<-")] SET = 40,

        [Sugar("->")] DIS_SET = 41,
    
        [Sugar( ">")] GREATER = 30,

        [Sugar( "<")] LESSER = 31,

        [Sugar( "==")] EQUALS = 32,

        [Sugar( "!=")] DIFFERENT = 33,

        [Sugar( ".")] CONCAT = 34,

        [Sugar( "-*")] DIRECT = 35,
    
        [Sugar("*-")] DIS_DIRECT = 43,

        [Sugar( "+")] PLUS = 36,

        [Sugar( "-")] MINUS = 37,
    
        [Sugar( "*")] TIMES = 38,

        [Sugar( "/")] DIVIDE = 39,
    
        [Sugar(",")]DOUHAO = 42,

        #endregion

        #region sugar 50 ->

        [Sugar( "(")] LPAREN = 50,

        [Sugar( ")")] RPAREN = 51,
        [Sugar("[")] L_BRACKET = 52,
        [Sugar("]")] R_BRACKET = 53,

        EOF = 0

        #endregion
}
```

这里声明一个元素需要使用到[Lexeme(`<GenericToken>,"<string>"`))]来定义，前者代表词素，后者代表指向的字符串。

仔细看你会发现这里有两种写法，我在这里讲一下：

长的写法：

```csharp
 [Lexeme(GenericToken.KeyWord, "IF")]
 [Lexeme(GenericToken.String)]
```

短的写法：

```csharp
[Keyword("IF"))]
[String] 
```

csly的lexer还支持注释：

```csharp
[Comment("//","/*","*/")]
        COMMENT = 18,
```

[Comment("<单行注释表示符>","<多行注释开端>"，“<多行注释结尾>”)]

可以自动支持注释而不需要写ebnf

现在，我们写出了一个可以被csly所识别的lexer
