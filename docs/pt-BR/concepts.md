# Conceitos

*[English](../concepts.md) · [Voltar às docs](README.md)*

Seis ideias sustentam a biblioteca inteira.

## Error

Uma falha de domínio descrita como dado: um `Code` estável, um `ErrorKind`, `Arguments` de interpolação e um `Field` opcional. Não é uma exceção e não carrega stack trace.

```csharp
var error = Error.NotFound("order", 42);
// Code      = "not_found"
// Kind      = ErrorKind.NotFound
// Arguments = { resource: "order", id: 42 }
// Field     = null
```

`Error` é imutável e compara por valor, então é seguro cachear, comparar em testes e passar adiante livremente:

```csharp
Error.NotFound("order", 42) == Error.NotFound("order", 42);   // true
```

As instâncias vêm apenas das factories estáticas — o construtor é interno. É isso que garante que todo erro do sistema tenha um formato conhecido e um código que algum catálogo consiga resolver.

## ErrorKind

O conjunto fechado de espécies de falha. Um kind decide duas coisas: o **status HTTP** e o **rank de severidade** usado para escolher um vencedor quando um resultado carrega vários erros.

Regras de negócio não inventam kinds. Elas reusam um e fornecem o próprio código:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
```

Esse é o trade central do design. Um conjunto fechado de kinds torna o mapeamento de transporte total — todo erro que o domínio pode produzir já tem um status definido, sem registro para manter e sem branch default para esquecer. Um espaço de códigos aberto deixa o domínio ser tão específico quanto quiser. Veja o [guia ASP.NET Core](aspnet-guide.md) para a tabela completa kind → status.

## Result e Result&lt;T&gt;

O desfecho de uma operação: sucesso (com um valor, no caso de `Result<T>`) ou falha carregando um ou mais erros. É assim que código de domínio e de aplicação reporta falha — devolvendo, não lançando.

```csharp
Result<Order> found   = Result<Order>.Success(order);
Result<Order> missing = Result<Order>.Failure(Error.NotFound("order", id));
Result        done    = Result.Success();
```

Ambos são `readonly struct`, então não há alocação no caminho de sucesso. Duas consequências que vale conhecer:

- `default(Result)` é **sucesso**. Um campo que você nunca atribuiu é lido como "nada deu errado".
- `Result.Failure()` com zero erros lança `ArgumentException`. Uma falha sem nada a reportar seria uma falha sobre a qual ninguém poderia agir.

Ler o valor é explícito — `Value` lança em um resultado falho, então use `TryGetValue`, `Match`, ou cheque `IsSuccess` antes.

## Erro primário

Quando um resultado carrega vários erros, um deles dirige a resposta: o erro do **kind mais severo**, com empates resolvidos a favor do **primeiro erro do resultado**. Ele fornece o `title` e o `detail` do Problem Details, e seu kind fornece o status HTTP.

Os demais erros não são descartados — todos aparecem no array `errors`. Um formulário que falha validação em três campos devolve `400` e reporta os três.

## Catálogo de mensagens

Um arquivo JSON por cultura mapeando `Code` → template de mensagem. Os metadados ficam em C#; só o texto é traduzido.

```json
{ "not_found": "{resource} '{id}' não foi encontrado." }
```

Os tokens são preenchidos a partir de `Error.Arguments`. A resolução caminha da cultura pedida para o pai e daí para o catálogo invariante, então `pt-BR` cai para `pt` e depois para o default. Detalhes em [Mensagens e culturas](messages.md).

## Escape hatch

`Error.ToException()` produz uma `DomainException` carregando os erros, para fronteiras cuja assinatura você não controla — um construtor, ou uma interface que você não escreveu.

```csharp
if (quantity <= 0)
    throw Error.Validation("quantity", attemptedValue: quantity).ToException();
```

Isso é a exceção, não a regra. Falhas de negócio comuns devolvem um `Result`. Recorrer a `ToException` rotineiramente abre mão da propriedade que torna toda a abordagem válida: a de que a assinatura de um método diz que ele pode falhar.

## Por que não exceções

Exceções são fluxo de controle para o *inesperado*. Um pedido inexistente, um e-mail duplicado, um token expirado — nada disso é inesperado; são desfechos que quem chamou deveria tratar. Modelá-los como valor de retorno significa:

- A assinatura é honesta. `Result<Order> Get(string id)` diz que a falha é possível; `Order Get(string id)` afirma que não.
- Várias falhas podem ser reportadas de uma vez. Uma exceção carrega uma.
- O mapeamento de transporte é dado, não uma cadeia de blocos `catch`.
- Nada desempilha a stack por causa de uma regra de negócio.

`ErrorKind.Unexpected` continua existindo para falhas genuínas — e é o único kind cujo detalhe nunca é mostrado ao cliente. Veja [tratamento de 500](aspnet-guide.md#erros-inesperados-e-500).
