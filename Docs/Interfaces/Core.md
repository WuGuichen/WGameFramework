# Core 接口

## 职责

Core 提供纯 C# 基础工具。`MxFramework.Core` 不依赖 UnityEngine；Unity 类型相关工具放在 `MxFramework.Core.Unity`。

## 公开类型

| 类型 | 位置 | 用途 |
|------|------|------|
| `IHeapItem<T>` / `Heap<T>` | `Core/Collections/Heap.cs` | 固定容量堆 |
| `IUnsortListItem` / `UnsortList<T>` | `Core/Collections/UnsortList.cs` | 支持延迟删除的无序列表 |
| `BitUtils` | `Core/Math/BitUtils.cs` | int 打包、拆包、位运算辅助 |
| `zstring` | `Core/Extensions/ZString.cs` | 低分配字符串拼接 |
| `RandomTable` | `Core.Unity/RandomTable.cs` | Unity 随机查表 |
| `VectorExtensions` | `Core.Unity/VectorExtensions.cs` | Vector 角度、方向、长度扩展 |

## 使用约定

- `Heap<T>` 的元素必须维护 `HeapIndex`。
- `UnsortList<T>.RemoveDelayed` 后需要调用 `Optimize` 才真正压缩列表。
- Unity 类型只能放在 `Core.Unity`。
- Core 不承载游戏 ID、属性含义或业务规则。

## 测试入口

`Assets/Scripts/MxFramework/Tests/Core/`
