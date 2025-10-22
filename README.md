# DBCheckAI
MySQL数据库命名规则检查的AI助手

## 📖 项目简介
DBCheckAI是一个专门用于检查MySQL数据库命名规范的智能工具，它能够自动连接数据库，分析表和字段命名，生成详细的合规性报告，并提供修复建议。该工具遵循行业最佳实践，帮助团队统一数据库命名风格，提高代码可维护性。

## ✨ 核心功能

- **智能连接**：通过Web界面安全连接到MySQL数据库
- **自动分析**：提取并分析数据库中的所有表和字段命名
- **规范检查**：验证命名是否符合小写下划线命名法(snake_case)等行业标准
- **AI报告生成**：生成详细的Markdown格式检查报告，包含以下信息：
  - 数据库结构摘要统计
  - 不合规命名项列表
  - 问题描述和建议名称
  - 自动生成的SQL修复语句
- **自定义规则**：支持用户自定义命名规范规则

## 🛠 技术栈

- **前端**：ASP.NET Core Razor Pages + Bootstrap
- **后端**：C# .NET 8.0
- **数据库**：MySQL (使用MySql.Data驱动)
- **报告生成**：Markdig (Markdown渲染)

## 🚀 快速开始

### 环境要求

- .NET 8.0 SDK
- MySQL 5.7+ 数据库服务器

### 安装与运行

1. 克隆或下载项目代码

```bash
git clone <repository-url>
cd MySQLDatabaseNamingChecker
```

2. 还原依赖包

```bash
dotnet restore
```

3. 构建项目

```bash
dotnet build
```

4. 运行应用

```bash
dotnet run
```

5. 在浏览器中访问应用

默认地址：`http://localhost:5000`

## 📋 使用方法

1. 在Web界面中，输入MySQL数据库的连接字符串

   ```
   Server=localhost;Database=your_database;Uid=username;Pwd=password;
   ```

2. 查看或自定义命名规则（默认为行业标准的小写下划线命名法）

3. 点击"开始检查"按钮，系统将：
   - 连接到指定数据库
   - 提取表和字段信息
   - 分析命名是否符合规范
   - 生成详细的检查报告

4. 查看生成的报告，报告包含：
   - 检查摘要（总表数、总字段数、不合规项等）
   - 不符合规范的命名列表
   - 问题描述和建议的修复名称
   - 可以直接执行的SQL修复语句

## 📏 默认命名规则

系统默认采用以下命名规范（可自定义）：

### 表名规范
- 使用复数名词，如 users, orders
- 使用小写 + 下划线：user_profile, order_item
- 不允许使用大写字母或驼峰
- 长度不超过64字符

### 字段名规范
- 使用小写下划线命名法：user_id, create_time, total_amount
- 主键必须叫 id
- 外键必须以 _id 结尾，如 user_id, order_id
- 创建时间字段叫 create_time，更新时间叫 update_time
- 布尔字段用 is_xxx，如 is_active, is_deleted

### 禁止项
- 使用拼音（如 yonghu）
- 使用MySQL保留字（如 order, user, group 等）
- 使用空格或特殊字符
- 使用MySQL不支持的字符

## ⚠️ 安全提示

- 连接字符串中的敏感信息（用户名、密码）仅在服务器端临时使用，不会被保存
- 建议在使用前备份数据库，特别是在执行自动生成的SQL修复语句时
- 生产环境中使用时，请确保有适当的访问权限控制

## 🔧 常见问题

### 无法连接到数据库
- 检查连接字符串格式是否正确
- 确认MySQL服务器是否正在运行且可访问
- 验证用户名和密码是否正确
- 检查数据库用户是否有足够权限访问information_schema

### 报告生成错误
- 检查数据库连接是否稳定
- 确认数据库中是否有表和数据
- 如遇到特殊字符导致的问题，可尝试修改命名规则

## 🤝 贡献指南

欢迎提交Issue和Pull Request来改进这个工具。如有任何问题或建议，请通过项目仓库的Issue功能提出。

## 📝 许可证

[MIT License](LICENSE)

---

*DBCheckAI - 让数据库命名规范检查变得简单高效！*