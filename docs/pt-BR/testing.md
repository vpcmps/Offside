# Guia de testes

*[English](../testing.md) · [Voltar para a documentação](README.md)*

`Offside.Testing` faz asserções sobre `Result`, `Result<T>`, `Error` e catálogos de mensagens. O ganho não é escrever menos código de teste — é o que você lê quando o teste quebra. `Assert.True(result.IsFailure)` reporta `Assert.True() Failure`; este pacote reporta quais erros o resultado realmente carregava.

## Instalação

```bash
dotnet add package Offside.Testing
```

O pacote não depende de nenhum framework de teste. As falhas são lançadas como `OffsideAssertionException`, e xUnit, NUnit, MSTest e TUnit tratam qualquer exceção como teste falho.

Os pontos de entrada se chamam `ShouldHaveError` e afins em vez de `Should()`, para que o pacote possa ser usado no mesmo arquivo que FluentAssertions ou Shouldly sem colidir com o ponto de entrada deles.

## Asserção sobre o resultado

```csharp
using Offside.Testing;

createOrder.Handle(command).ShouldBeSuccess();

var order = getOrder.Handle(query).ShouldBeSuccess().Subject;
getOrder.Handle(query).ShouldBeSuccess().WithValue(o => o.Status == OrderStatus.Paid, "pago");
```

| Asserção | Significa |
|---|---|
| `ShouldBeSuccess()` | O resultado teve sucesso. Em `Result<T>`, expõe o valor para refinamento. |
| `ShouldBeFailure()` | O resultado falhou, sem dizer como. |
| `ShouldHaveError(code)` | Falhou carregando este erro, ignorando os demais. **A escolha padrão.** |
| `ShouldHaveOnlyError(code)` | Falhou carregando este erro e mais nenhum. |
| `ShouldHaveErrorsInOrder(codes)` | Carrega exatamente estes códigos, nesta ordem. |
| `ShouldHaveErrorCount(n)` | Carrega esta quantidade de erros, sem dizer quais. |

`ShouldHaveError` é o padrão. Ele sobrevive a uma regra nova adicionada em outro ponto do fluxo. `ShouldHaveOnlyError` é o irmão estrito: use onde um erro a mais vazando é justamente o defeito que você quer pegar.

`ShouldHaveErrorsInOrder` fixa a ordem, que vem da origem: ordem dos argumentos no `Result.Combine`, e ordem de declaração das regras para erros vindos do FluentValidation. Reordenar regras num validator quebra essa asserção sem que nenhum comportamento tenha mudado — use apenas quando a ordem for exatamente o que você quer afirmar.

## Refinando o erro

```csharp
result.ShouldHaveError("order.duplicated")
      .WithKind(ErrorKind.Conflict)
      .WithErrorCode("CONFLICT")
      .ForField("number")
      .WithArgument("number", "A-1");
```

O erro é localizado primeiro, então a falha diz qual parte divergiu — "o erro existe, mas o kind é `Conflict`, esperado `Validation`" — em vez de "nenhum erro correspondeu".

`WithMessage` resolve a mensagem através de um resolver que você passa:

```csharp
result.ShouldHaveError("not_found").WithMessage(resolver, "order 42 was not found");
result.ShouldHaveError("not_found").WithMessage(resolver, new CultureInfo("pt-BR"), "pedido 42 não encontrado");
```

Atenção: o resolver embutido devolve `Error.Code` quando o catálogo não tem entrada para ele, então um `WithMessage` que passa não prova que o catálogo define o código. Quem prova isso é o `OffsideCatalog`.

## Encadeamento

`.And` devolve o resultado original:

```csharp
result.ShouldHaveError("user.email_invalid").ForField("email")
      .And.ShouldHaveError("user.age_invalid").ForField("age");
```

É sempre opcional — começar uma nova instrução sobre o mesmo resultado faz o mesmo, e costuma ler melhor:

```csharp
createResult.ShouldHaveError("order.duplicated").ForField("number");
updateResult.ShouldBeSuccess().WithValue(o => o.Status == OrderStatus.Paid);
```

## Asserção sobre o catálogo

Um código sem entrada no catálogo é um defeito que só aparece em runtime: o resolver cai de volta no próprio código, e o usuário vê `order.not_found` onde deveria haver uma frase. O `OffsideCatalog` lê o JSON diretamente e transforma isso em falha de build.

```csharp
var catalog = OffsideCatalog.FromFile("errors/errors.json");

catalog.ShouldDefine("order.not_found");
catalog.ShouldDefineAll("order.not_found", "order.duplicated", "order.already_shipped");
catalog.ShouldResolve(Error.NotFound("order", 42));
```

`ShouldResolve` é o mais forte: verifica que o código existe **e** que nenhum `{token}` ficou sem ser preenchido pelos `Error.Arguments`. Um template `"{resource} {id} was not found"` resolvido contra um erro sem o argumento `id` falha aqui, nomeando o token que sobrou.

Mantendo um catálogo traduzido honesto:

```csharp
var invariant = OffsideCatalog.FromFile("errors/errors.json");
var translated = OffsideCatalog.FromFile("errors/errors.pt-BR.json");

translated.ShouldDefineSameCodesAs(invariant);
```

`FromJson`, `FromStream` e `FromAssembly` cobrem catálogos que não estão em disco — recursos embutidos, ou conteúdo vindo do Azure App Configuration e materializado no teste.

## O que cobrir

Um mínimo útil para um fluxo que devolve `Result`:

- **Todo caminho de falha tem teste.** Uma regra sem teste é uma regra que pode parar de disparar em silêncio — e com `Result`, nada estoura para te avisar.
- **Todo código que um handler pode retornar está definido no catálogo.** Um `ShouldDefineAll` num único teste cobre o fluxo inteiro.
- **Todo código com template contendo `{token}` é verificado com `ShouldResolve`**, usando um erro construído do jeito que produção constrói. É o check que pega um argumento renomeado só de um lado.
- **Catálogos traduzidos são comparados com o invariante** via `ShouldDefineSameCodesAs`, um teste por cultura.
- **Assere o kind, não só o code**, onde o kind decide o status HTTP. `WithKind` é o que impede um 409 de virar 422 em silêncio.
- Prefira `ShouldHaveError` em geral; reserve `ShouldHaveOnlyError` para onde um erro extra é o bug em si.

## Usando junto com FluentAssertions

Os dois convivem no mesmo teste. As asserções do Offside carregam o vocabulário do domínio; a biblioteca de uso geral cobre o resto:

```csharp
var order = handler.Handle(query).ShouldBeSuccess().Subject;

order.Lines.Should().HaveCount(3);
```

`Subject` existe exatamente para essa passagem de bastão — expõe o `Error` localizado ou o valor de sucesso para outra biblioteca assumir.
