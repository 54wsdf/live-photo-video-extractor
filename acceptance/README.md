# 真实照片验收

`run-real-sample.ps1` 使用发布后的独立 EXE 验证真实 Motion Photo，但不会在原照片旁写入任何内容。

脚本会：

1. 要求一个尚不存在的临时目录。
2. 计算原 JPG 的 SHA-256，然后复制到临时目录。
3. 用发布后的 EXE 从副本中提取 MP4。
4. 检查 MP4 的 `ftyp`、`moov`、`mdat` 顶层结构。
5. 检查输出时间戳，并再次确认原 JPG 哈希没有变化。

示例：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\acceptance\run-real-sample.ps1 `
  -SourceJpeg 'D:\Photos\MVIMG_example.jpg' `
  -ScratchDirectory '.\artifacts\acceptance-real'
```
