# Guia de domínio

*[English](../domain-guide.md) · [Voltar às docs](README.md)*

Como escrever código de domínio e de aplicação com `Result`. Tudo nesta página vem apenas do pacote `Offside` — sem dependência de ASP.NET.

## Devolvendo um resultado

```csharp
using Offside;

public Result<Order> Get(string id)
{
    var order = _orders.Find(id);
    return order is null
        ? Result<Order>.Failure(Error.NotFound("order", id))
        : Result<Order>.Success(order);
}
```

Para uma operação sem valor de retorno, use o `Result` não genérico:

```csharp
public Result Cancel(string id)
{
    var order = _orders.Find(id);
    if (order is null)
        return Result.Failure(Error.NotFound("order", id));

    if (order.Shipped)
        return Result.Failure(Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId = id }));

    order.Cancel();
    return Result.Success();
}
```

## Factories de erro

Cada factory produz um `ErrorKind` específico e um código de catálogo padrão.

| Factory | Kind | Código | Argumentos |
|---|---|---|---|
| `Error.NotFound(resource, id?)` | `NotFound` | `not_found` | `resource`, `id` |
| `Error.Gone(resource, id?)` | `Gone` | `gone` | `resource`, `id` |
| `Error.Conflict(resource, reason?)` | `Conflict` | `conflict` | `resource`, `reason` |
| `Error.Validation(field, code?, attemptedValue?)` | `Validation` | `validation`, ou `code` | `field`, `attemptedValue` |
| `Error.BadRequest(reason?)` | `BadRequest` | `bad_request` | `reason` |
| `Error.Unauthorized(reason?)` | `Unauthorized` | `unauthorized` | `reason` |
| `Error.Forbidden(reason?)` | `Forbidden` | `forbidden` | `reason` |
| `Error.PreconditionFailed(reason?)` | `PreconditionFailed` | `precondition_failed` | `reason` |
| `Error.Unprocessable(reason?)` | `Unprocessable` | `unprocessable` | `reason` |
| `Error.TooManyRequests(reason?)` | `TooManyRequests` | `too_many_requests` | `reason` |
| `Error.Unexpected(detail?)` | `Unexpected` | `unexpected` | `detail` |
| `Error.Custom(code, kind, arguments?, field?)` | *sua escolha* | *sua escolha* | *sua escolha* |

`Error.Validation` é a única factory que preenche `Field`, e a única que permite sobrescrever o código sem passar por `Custom`:

```csharp
Error.Validation("email");                                 // código "validation", field "email"
Error.Validation("email", "email.malformed", input);       // código "email.malformed", field "email"
```

## Erros customizados para regras de negócio

Quando uma regra merece a própria mensagem, mantenha o kind e invente o código:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId });
Error.Custom("payment.insufficient_funds", ErrorKind.Unprocessable, new { required, available });
Error.Custom("coupon.expired", ErrorKind.PreconditionFailed, new { coupon = code, expiredOn }, field: "coupon");
```

O código vira a chave do catálogo, então adicione a entrada correspondente em todos os catálogos:

```json
{ "order.already_shipped": "O pedido {orderId} já foi enviado." }
```

Um código vazio ou só com espaços lança `ArgumentException`. Espaços nas pontas são removidos.

Vale adotar uma convenção pontuada e com namespace (`order.already_shipped`): códigos são contrato público sobre o qual clientes fazem branch, e um namespace plano colide mais cedo do que se espera.

## Argumentos

`Arguments` é um snapshot somente leitura tirado na construção. Objetos anônimos e dicionários funcionam:

```csharp
Error.Custom("quota.exceeded", ErrorKind.TooManyRequests, new { limit = 100, window = "1h" });
Error.Custom("quota.exceeded", ErrorKind.TooManyRequests, new Dictionary<string, object?> { ["limit"] = 100 });
```

Duas regras:

- **Argumentos alimentam templates de mensagem.** Um token `{limit}` no catálogo é preenchido a partir da entrada `limit`.
- **Nunca coloque segredos ali.** Eles moldam texto que vai para o cliente. Tokens, hashes de senha e connection strings internas não pertencem a esse lugar — material de diagnóstico vai em `Error.Unexpected(detail)`, que é [sanitizado na saída](aspnet-guide.md#erros-inesperados-e-500).

## Lendo um resultado

`Value` lança `InvalidOperationException` em um resultado falho. Escolha o que couber:

```csharp
// Pattern match para um único valor
var message = result.Match(
    order => $"Encontrado {order.Id}",
    errors => $"Falhou: {errors[0].Code}");

// Estilo try
if (result.TryGetValue(out var order))
    Console.WriteLine(order.Id);

// Checagem explícita
if (result.IsSuccess)
    Use(result.Value);
```

## Compondo

`Map` transforma um valor. `Bind` encadeia uma operação que também pode falhar. Ambos fazem short-circuit — o delegate nunca roda em um resultado falho, e os erros originais passam intactos:

```csharp
Result<OrderDto> dto = _orders.Get(id)
    .Bind(order => _pricing.Apply(order))   // Result<Order>
    .Map(order => OrderDto.From(order));    // valor simples
```

`Result.Combine` funde resultados independentes, concatenando os erros na ordem dos argumentos. É assim que se reporta todas as falhas de validação de uma vez, em vez de uma por round-trip:

```csharp
var combined = Result.Combine(
    ValidateEmail(request.Email),
    ValidateName(request.Name),
    ValidateAge(request.Age));

// Se as três falharem → combined.Errors tem três entradas, nessa ordem.
```

Existe uma sobrecarga `Combine<T>(params Result<T>[])` para resultados com valor; ela funde os erros e descarta os valores, devolvendo um `Result` unitário.

## Ausências deliberadas

Duas coisas que você pode procurar e não encontrar:

- **Sem conversão implícita de `T` para `Result<T>`.** Construir um resultado é sempre explícito, então um valor nunca vira sucesso por acidente — e um refactor que muda um tipo de retorno gera erro de compilação em vez de comportamento silencioso.
- **Sem `Apply`.** `Combine` já cobre o caso de acumular todos os erros, sem uma segunda forma sutilmente diferente de fazer a mesma coisa.

## O escape hatch

Para fronteiras que não podem devolver um `Result` — um construtor, uma interface que não é sua:

```csharp
public Order(string id, int quantity)
{
    if (quantity <= 0)
        throw Error.Validation("quantity", attemptedValue: quantity).ToException();
    ...
}
```

`DomainException.Errors` leva os erros adiante, então um handler de fronteira ainda consegue renderizá-los corretamente. Use com parcimônia: uma base onde metade das falhas lança perdeu a garantia de que a assinatura diz o que pode dar errado.

## Testes

Erros comparam por valor, então as asserções ficam diretas:

```csharp
var result = service.Get("missing");

Assert.True(result.IsFailure);
Assert.Equal(Error.NotFound("order", "missing"), result.Errors[0]);
```

Faça asserção sobre `Code` e `Kind`, não sobre o texto resolvido — o texto é dado de catálogo e deve poder mudar sem quebrar nada.
