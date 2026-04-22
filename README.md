# VSL

Vintage Story 服务器启动器（Windows / WPF）。

## 功能概览

- 版本安装与本地 ZIP 导入
- 档案管理与配置编辑
- 存档管理与 Mod 管理
- 控制台日志、命令发送、日志下载

## 运行与构建

```powershell
dotnet build VSL.sln -c Release
```

## 打包

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1 -Version 1.0.0
```

生成安装包（`setup.exe`）：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package.ps1 -Version 1.0.0 -CreateInstaller
```

说明：

- `-CreateInstaller` 使用 Inno Setup 6 生成安装程序。
- 需先安装 Inno Setup 6（`ISCC.exe`）。
- 默认安装目录为 `%LocalAppData%\Programs\VSL`（无需管理员权限）。

## 关于

- 作者：寒士杰克（HansJack）
- 制作组：复古物语中文社区（vintagestory.top）
- 项目仓库：https://github.com/VintageStory-Community/VSL
