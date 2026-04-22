# conventions

## 新增 Demo 规则

1. 所有 Demo 放在 `samples/` 下
2. 每个 Demo 必须包含 `src/`、`tests/`、`README.md`
3. 公共能力优先沉淀到 `shared/`
4. 所有项目必须加入 `dotnet-demo-hub.sln`

## 测试规范

- 所有 Demo 默认包含 `tests/` 目录
- 模板级、公共组件型、业务逻辑型 Demo 必须补充测试用例
- 纯演示型、一次性验证型 Demo 可只保留最小测试或测试说明
- 对外部依赖较强的 Demo，优先考虑最小可运行验证，而不是堆砌高维护成本测试

## 命名规范
- Demo 目录：`XXXDemo`
- 主项目：`Demo.XXX`
- 测试项目：`Demo.XXX.Tests`