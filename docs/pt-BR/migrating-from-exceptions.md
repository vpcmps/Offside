# Migração de exceções de domínio

1. Registre a Offside e as opções ASP.NET Core, adicione `services.AddOffsideExceptionHandler()` e chame `app.UseOffsideExceptionHandler()` antes dos endpoints.
2. Troque a exceção própria por `throw Error.NotFound("order", id).ToException()`. Se o handler antigo ainda tratar as exceções comuns, configure `HandleUnexpectedExceptions = false`.
3. Comece pelos casos de uso mais acessados. Retorne um `Result` de falha e renderize com `ToHttpResult(http)`. O formato da resposta de erro é o mesmo.
4. Remova os tipos de exceção e o handler antigo quando os chamadores restantes tiverem migrado.
