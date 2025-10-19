# 📊 PostgreSQL数据库命名规范检查报告

📅 生成时间：2025-10-19 18:00:34

## 📌 检查摘要

- **总表数**：8
- **总字段数**：54
- **不合规项**：106
- **合规率**：-96%

## ❌ 不符合规范的命名

| 类型 | 对象 | 当前名称 | 问题 | 建议名称 |
|------|------|----------|------|----------|
| 字段 | `__efmigrationshistory.MigrationId` | `MigrationId` | 字段名应使用小写+下划线命名法 | `migration_id` |
| 字段 | `__efmigrationshistory.ProductVersion` | `ProductVersion` | 字段名应使用小写+下划线命名法 | `product_version` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Username` | `Username` | 字段名应使用小写+下划线命名法 | `username` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.PasswordHash` | `PasswordHash` | 字段名应使用小写+下划线命名法 | `password_hash` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Role` | `Role` | 字段名应使用小写+下划线命名法 | `role` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.CreatedAt` | `CreatedAt` | 字段名应使用小写+下划线命名法 | `created_at` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Avatar` | `Avatar` | 字段名应使用小写+下划线命名法 | `avatar` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.CreatedBy` | `CreatedBy` | 字段名应使用小写+下划线命名法 | `created_by` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.DeletedAt` | `DeletedAt` | 字段名应使用小写+下划线命名法 | `deleted_at` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Email` | `Email` | 字段名应使用小写+下划线命名法 | `email` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.IsDeleted` | `IsDeleted` | 字段名应使用小写+下划线命名法 | `is_deleted` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.NickName` | `NickName` | 字段名应使用小写+下划线命名法 | `nick_name` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Phone` | `Phone` | 字段名应使用小写+下划线命名法 | `phone` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.Status` | `Status` | 字段名应使用小写+下划线命名法 | `status` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.UpdatedAt` | `UpdatedAt` | 字段名应使用小写+下划线命名法 | `updated_at` |
| 表名 | `adminusers` | `adminusers` | 表名应使用小写+下划线命名法 | `adminusers` |
| 字段 | `adminusers.UpdatedBy` | `UpdatedBy` | 字段名应使用小写+下划线命名法 | `updated_by` |
| 表名 | `blacklistedtokens` | `blacklistedtokens` | 表名应使用小写+下划线命名法 | `blacklistedtokens` |
| 字段 | `blacklistedtokens.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `blacklistedtokens` | `blacklistedtokens` | 表名应使用小写+下划线命名法 | `blacklistedtokens` |
| 字段 | `blacklistedtokens.Token` | `Token` | 字段名应使用小写+下划线命名法 | `token` |
| 表名 | `blacklistedtokens` | `blacklistedtokens` | 表名应使用小写+下划线命名法 | `blacklistedtokens` |
| 字段 | `blacklistedtokens.ExpiredAt` | `ExpiredAt` | 字段名应使用小写+下划线命名法 | `expired_at` |
| 表名 | `blacklistedtokens` | `blacklistedtokens` | 表名应使用小写+下划线命名法 | `blacklistedtokens` |
| 字段 | `blacklistedtokens.CreatedAt` | `CreatedAt` | 字段名应使用小写+下划线命名法 | `created_at` |
| 表名 | `categories` | `categories` | 表名应使用小写+下划线命名法 | `categories` |
| 字段 | `categories.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `categories` | `categories` | 表名应使用小写+下划线命名法 | `categories` |
| 字段 | `categories.Name` | `Name` | 字段名应使用小写+下划线命名法 | `name` |
| 表名 | `categories` | `categories` | 表名应使用小写+下划线命名法 | `categories` |
| 字段 | `categories.Slug` | `Slug` | 字段名应使用小写+下划线命名法 | `slug` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.Title` | `Title` | 字段名应使用小写+下划线命名法 | `title` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.Summary` | `Summary` | 字段名应使用小写+下划线命名法 | `summary` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.Content` | `Content` | 字段名应使用小写+下划线命名法 | `content` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.CoverImage` | `CoverImage` | 字段名应使用小写+下划线命名法 | `cover_image` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.IsFeatured` | `IsFeatured` | 字段名应使用小写+下划线命名法 | `is_featured` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.PublishDate` | `PublishDate` | 字段名应使用小写+下划线命名法 | `publish_date` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.CategoryId` | `CategoryId` | 字段名应使用小写+下划线命名法 | `category_id` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.CreatedAt` | `CreatedAt` | 字段名应使用小写+下划线命名法 | `created_at` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.CreatedBy` | `CreatedBy` | 字段名应使用小写+下划线命名法 | `created_by` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.DeletedAt` | `DeletedAt` | 字段名应使用小写+下划线命名法 | `deleted_at` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.IsDeleted` | `IsDeleted` | 字段名应使用小写+下划线命名法 | `is_deleted` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.UpdatedAt` | `UpdatedAt` | 字段名应使用小写+下划线命名法 | `updated_at` |
| 表名 | `news` | `news` | 表名应使用小写+下划线命名法 | `news` |
| 字段 | `news.UpdatedBy` | `UpdatedBy` | 字段名应使用小写+下划线命名法 | `updated_by` |
| 表名 | `newstag` | `newstag` | 表名应使用小写+下划线命名法 | `newstag` |
| 字段 | `newstag.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `newstag` | `newstag` | 表名应使用小写+下划线命名法 | `newstag` |
| 字段 | `newstag.NewsId` | `NewsId` | 字段名应使用小写+下划线命名法 | `news_id` |
| 表名 | `newstag` | `newstag` | 表名应使用小写+下划线命名法 | `newstag` |
| 字段 | `newstag.TagId` | `TagId` | 字段名应使用小写+下划线命名法 | `tag_id` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.Name` | `Name` | 字段名应使用小写+下划线命名法 | `name` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.Code` | `Code` | 字段名应使用小写+下划线命名法 | `code` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.Description` | `Description` | 字段名应使用小写+下划线命名法 | `description` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.CreateTime` | `CreateTime` | 字段名应使用小写+下划线命名法 | `create_time` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.CreatedAt` | `CreatedAt` | 字段名应使用小写+下划线命名法 | `created_at` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.UpdatedAt` | `UpdatedAt` | 字段名应使用小写+下划线命名法 | `updated_at` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.IsDeleted` | `IsDeleted` | 字段名应使用小写+下划线命名法 | `is_deleted` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.DeletedAt` | `DeletedAt` | 字段名应使用小写+下划线命名法 | `deleted_at` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.CreatedBy` | `CreatedBy` | 字段名应使用小写+下划线命名法 | `created_by` |
| 表名 | `roles` | `roles` | 表名应使用小写+下划线命名法 | `roles` |
| 字段 | `roles.UpdatedBy` | `UpdatedBy` | 字段名应使用小写+下划线命名法 | `updated_by` |
| 表名 | `tags` | `tags` | 表名应使用小写+下划线命名法 | `tags` |
| 字段 | `tags.Id` | `Id` | 字段名应使用小写+下划线命名法 | `id` |
| 表名 | `tags` | `tags` | 表名应使用小写+下划线命名法 | `tags` |
| 字段 | `tags.Name` | `Name` | 字段名应使用小写+下划线命名法 | `name` |

## 🔧 建议的 SQL 修复语句
```sql
-- 修复字段: ALTER TABLE "__efmigrationshistory" RENAME COLUMN "MigrationId" TO "migration_id";
-- 修复字段: ALTER TABLE "__efmigrationshistory" RENAME COLUMN "ProductVersion" TO "product_version";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Username" TO "username";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "PasswordHash" TO "password_hash";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Role" TO "role";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "CreatedAt" TO "created_at";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Avatar" TO "avatar";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "CreatedBy" TO "created_by";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "DeletedAt" TO "deleted_at";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Email" TO "email";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "IsDeleted" TO "is_deleted";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "NickName" TO "nick_name";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Phone" TO "phone";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "Status" TO "status";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "UpdatedAt" TO "updated_at";
-- 修复表名: ALTER TABLE "adminusers" RENAME TO "adminusers";
-- 修复字段: ALTER TABLE "adminusers" RENAME COLUMN "UpdatedBy" TO "updated_by";
-- 修复表名: ALTER TABLE "blacklistedtokens" RENAME TO "blacklistedtokens";
-- 修复字段: ALTER TABLE "blacklistedtokens" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "blacklistedtokens" RENAME TO "blacklistedtokens";
-- 修复字段: ALTER TABLE "blacklistedtokens" RENAME COLUMN "Token" TO "token";
-- 修复表名: ALTER TABLE "blacklistedtokens" RENAME TO "blacklistedtokens";
-- 修复字段: ALTER TABLE "blacklistedtokens" RENAME COLUMN "ExpiredAt" TO "expired_at";
-- 修复表名: ALTER TABLE "blacklistedtokens" RENAME TO "blacklistedtokens";
-- 修复字段: ALTER TABLE "blacklistedtokens" RENAME COLUMN "CreatedAt" TO "created_at";
-- 修复表名: ALTER TABLE "categories" RENAME TO "categories";
-- 修复字段: ALTER TABLE "categories" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "categories" RENAME TO "categories";
-- 修复字段: ALTER TABLE "categories" RENAME COLUMN "Name" TO "name";
-- 修复表名: ALTER TABLE "categories" RENAME TO "categories";
-- 修复字段: ALTER TABLE "categories" RENAME COLUMN "Slug" TO "slug";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "Title" TO "title";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "Summary" TO "summary";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "Content" TO "content";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "CoverImage" TO "cover_image";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "IsFeatured" TO "is_featured";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "PublishDate" TO "publish_date";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "CategoryId" TO "category_id";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "CreatedAt" TO "created_at";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "CreatedBy" TO "created_by";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "DeletedAt" TO "deleted_at";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "IsDeleted" TO "is_deleted";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "UpdatedAt" TO "updated_at";
-- 修复表名: ALTER TABLE "news" RENAME TO "news";
-- 修复字段: ALTER TABLE "news" RENAME COLUMN "UpdatedBy" TO "updated_by";
-- 修复表名: ALTER TABLE "newstag" RENAME TO "newstag";
-- 修复字段: ALTER TABLE "newstag" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "newstag" RENAME TO "newstag";
-- 修复字段: ALTER TABLE "newstag" RENAME COLUMN "NewsId" TO "news_id";
-- 修复表名: ALTER TABLE "newstag" RENAME TO "newstag";
-- 修复字段: ALTER TABLE "newstag" RENAME COLUMN "TagId" TO "tag_id";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "Name" TO "name";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "Code" TO "code";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "Description" TO "description";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "CreateTime" TO "create_time";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "CreatedAt" TO "created_at";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "UpdatedAt" TO "updated_at";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "IsDeleted" TO "is_deleted";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "DeletedAt" TO "deleted_at";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "CreatedBy" TO "created_by";
-- 修复表名: ALTER TABLE "roles" RENAME TO "roles";
-- 修复字段: ALTER TABLE "roles" RENAME COLUMN "UpdatedBy" TO "updated_by";
-- 修复表名: ALTER TABLE "tags" RENAME TO "tags";
-- 修复字段: ALTER TABLE "tags" RENAME COLUMN "Id" TO "id";
-- 修复表名: ALTER TABLE "tags" RENAME TO "tags";
-- 修复字段: ALTER TABLE "tags" RENAME COLUMN "Name" TO "name";
```
