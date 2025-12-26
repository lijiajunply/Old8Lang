# Old8Lang.SerializationLib

Old8Lang 序列化库，提供高性能的数据序列化功能。

## 功能特性

- **MessagePack 序列化**: 高性能、紧凑的二进制序列化格式
- **Protocol Buffers 序列化**: Google 的数据交换格式
- **统一接口**: 提供统一的序列化器接口，方便切换不同的序列化格式
- **扩展方法**: 便捷的扩展方法，简化序列化操作
- **文件和流支持**: 支持序列化到文件和流
- **JSON 转换**: 支持序列化结果与 JSON 相互转换

## 安装

在项目中引用 Old8Lang.SerializationLib:

```bash
dotnet add reference ../Old8Lang.SerializationLib/Old8Lang.SerializationLib.csproj
```

## 使用示例

### 基本用法

```csharp
using Old8Lang.SerializationLib;

// 创建序列化器
var serializer = SerializerFactory.Create(SerializationFormat.MessagePack);

// 序列化对象
var data = new { Name = "Old8Lang", Version = 1.0 };
byte[] bytes = serializer.Serialize(data);

// 反序列化对象
var result = serializer.Deserialize<dynamic>(bytes);
```

### 使用扩展方法

```csharp
using Old8Lang.SerializationLib;

var data = new Person { Name = "张三", Age = 25 };

// 序列化到字节数组
byte[] bytes = data.ToBytes();

// 从字节数组反序列化
var person = bytes.FromBytes<Person>();

// 序列化到文件
data.SaveToFile("person.msgpack");

// 从文件反序列化
var loaded = SerializationExtensions.LoadFromFile<Person>("person.msgpack");

// 深度克隆对象
var clone = data.DeepClone();
```

### MessagePack 序列化

```csharp
using Old8Lang.SerializationLib;

var serializer = new MessagePackSerializer();

// 序列化
var obj = new { Id = 1, Name = "Test", Items = new[] { 1, 2, 3 } };
byte[] data = serializer.Serialize(obj);

// 反序列化
var result = serializer.Deserialize<dynamic>(data);

// 转换为 JSON（调试用）
string json = serializer.ToJson(obj);
Console.WriteLine(json);

// 序列化到文件
serializer.SerializeToFile(obj, "data.msgpack");

// 从文件反序列化
var loaded = serializer.DeserializeFromFile<dynamic>("data.msgpack");
```

### Protocol Buffers 序列化

**注意**: 使用 Protobuf 需要先定义 `.proto` 文件并使用 `protoc` 生成 C# 类。

```proto
syntax = "proto3";

message Person {
  string name = 1;
  int32 age = 2;
  repeated string tags = 3;
}
```

生成类后使用：

```csharp
using Old8Lang.SerializationLib;

var serializer = new ProtobufSerializer();

// 创建 Protobuf 消息对象
var person = new Person
{
    Name = "张三",
    Age = 25,
    Tags = { "开发者", "架构师" }
};

// 序列化
byte[] data = serializer.Serialize(person);

// 反序列化
var result = serializer.Deserialize<Person>(data);

// 转换为 JSON
string json = serializer.ToJson(person);

// 从 JSON 转换
var fromJson = serializer.FromJson<Person>(json);
```

## 在 Old8Lang 语言中使用

库提供了 `SerializationLibBinding` 类，可以在 Old8Lang 语言中使用：

```csharp
// MessagePack 序列化
byte[] data = SerializationLibBinding.MsgPackSerialize(obj);
object result = SerializationLibBinding.MsgPackDeserialize(data, typeof(MyType));

// 序列化到文件
SerializationLibBinding.MsgPackSerializeToFile(obj, "data.msgpack");
object loaded = SerializationLibBinding.MsgPackDeserializeFromFile("data.msgpack", typeof(MyType));

// 转换为 JSON
string json = SerializationLibBinding.MsgPackToJson(obj);

// 深度克隆
object clone = SerializationLibBinding.DeepClone(obj);
```

## 性能对比

| 格式 | 序列化速度 | 反序列化速度 | 数据大小 | 特点 |
|------|-----------|-------------|---------|------|
| MessagePack | 快 | 快 | 小 | 无需预定义格式，支持动态类型 |
| Protobuf | 快 | 快 | 最小 | 需要预定义 .proto 文件，类型安全 |
| JSON | 慢 | 慢 | 大 | 人类可读，广泛支持 |

## 选择建议

- **MessagePack**: 推荐用于大多数场景，特别是需要动态类型或不想维护 .proto 文件的情况
- **Protobuf**: 推荐用于跨语言通信、需要严格类型定义和最小数据大小的场景

## 依赖项

- [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp) - MessagePack 序列化实现
- [Google.Protobuf](https://github.com/protocolbuffers/protobuf) - Protocol Buffers 实现

## 测试

运行测试：

```bash
dotnet test Old8Lang.SerializationLib.Tests/Old8Lang.SerializationLib.Tests.csproj
```

## 许可证

与 Old8Lang 项目相同
