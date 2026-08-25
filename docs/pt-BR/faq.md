# FAQ

*[English](../faq.md) · [Voltar às docs](README.md)*

## Por que `Result` em vez de exceções?

Exceções são para o inesperado. Um pedido inexistente ou um e-mail duplicado não é inesperado — é um desfecho que quem chamou precisa tratar. Devolvê-lo faz com que a assinatura seja honesta (`Result<Order> Get(string id)` admite falha; `Order Get(string id)` não), várias falhas possam ser reportadas de uma vez, e o mapeamento de transporte seja dado em vez de uma cadeia de blocos `catch`.

`ErrorKind.Unexpected` continua existindo para falhas genuínas, e recebe [tratamento especial](aspnet-guide.md#erros-inesperados-e-500).

## Por que não há conversão implícita de `T` para `Result<T>`?

Para que um valor nunca vire sucesso por acidente. Com conversão implícita, mudar o tipo de retorno de um método para `Result<T>` compila em silêncio e todo `return value;` existente continua funcionando — inclusive aqueles que agora deveriam ser falhas. Construção explícita transforma isso em um erro de compilação que você é obrigado a olhar.

## Por que não posso definir meu próprio `ErrorKind`?

Porque um conjunto fechado de kinds torna o mapeamento HTTP total. Todo erro que o domínio pode produzir já tem um status definido — sem registro para manter sincronizado, sem branch default para esquecer, sem um `500` porque alguém adicionou um kind e passou batido em um switch. A biblioteca pode **acrescentar** kinds (como fez com `ServiceUnavailable` e `Timeout`); o consumidor ainda não inventa os seus.

A especificidade vive no espaço de códigos, que é aberto:

```csharp
Error.Custom("order.already_shipped", ErrorKind.Conflict, new { orderId }, errorCode: "ORDER_ALREADY_SHIPPED");
```

Clientes decidem pelo `errorCode`. O kind só define o status e o rank de severidade. `code` continua sendo a chave do catálogo de mensagens.

## Por que o status vem do erro mais severo e não do primeiro?

Para que uma falha genuína nunca seja mascarada por uma mensagem de validação que por acaso foi listada primeiro. Um resultado carregando um erro de `Validation` e um de `Unexpected` é um 500, não um 400.

Nada se perde: todos os erros são enviados no array `errors` de qualquer forma. Empates dentro de um rank — `Unauthorized` e `Forbidden`, ou `Validation` e `BadRequest` — vão para o primeiro erro do resultado.

## Como devolvo `201 Created`?

Faça o branch antes de converter. `ToHttpResult` cuida do caminho de falha; você mantém o de sucesso:

```csharp
var result = handler.Handle(cmd);
return result.IsSuccess
    ? Results.Created($"/orders/{result.Value.Id}", result.Value)
    : result.ToHttpResult(http);
```

## `AddOffside` lança na inicialização. Por quê?

Você não registrou um catálogo de cultura invariante:

```csharp
options.AddJsonFile(CultureInfo.InvariantCulture, "errors/errors.json");
```

Ele é o fallback final de toda busca, então é obrigatório. Falhar no boot é deliberado — a alternativa é descobrir na primeira requisição com erro em produção.

## Minhas mensagens voltam como códigos crus, tipo `not_found`

O resolver não achou um template e devolveu o código. Verifique se:

- O código tem entrada no catálogo — códigos customizados precisam ser adicionados à mão.
- O arquivo de catálogo realmente chega ao diretório de saída (`<None Update="errors\*.json" CopyToOutputDirectory="PreserveNewest" />`).
- Você passou o **conteúdo** do arquivo para `AddJson`, não o caminho. Prefira `AddJsonFile`, que nomeia um arquivo ausente na inicialização.

## Um `{token}` aparece literalmente na mensagem

O template referenciou um argumento que o erro não carrega, ou carrega como nulo. `Error.NotFound("order")` contra `"{resource} '{id}' não foi encontrado."` produz `order '{id}' não foi encontrado.` — o argumento `id` é nulo e é pulado, não zerado. Passe o argumento, ou remova o token do template.

## Quando devo usar `DomainException`?

Só onde a assinatura está fora do seu controle — um construtor, ou uma interface que você não escreveu:

```csharp
throw Error.Validation("quantity", attemptedValue: quantity).ToException();
```

Se metade das falhas de uma base lança, a garantia de que a assinatura diz o que pode dar errado se perde, e com ela a maior parte da razão de usar esta biblioteca.

## Como adiciono um idioma?

Copie o catálogo, traduza os valores, registre. Traduções parciais são aceitáveis — o que faltar cai para a cultura pai e depois para o catálogo invariante. Veja [Mensagens e culturas](messages.md#adicionando-um-idioma).

## De onde vem a cultura?

Do header `Accept-Language` da requisição — primeiro range, sem quality values — a menos que você passe uma explicitamente. Um valor ausente, vazio, `*` ou não reconhecido cai para `CultureInfo.CurrentUICulture`. Um header malformado nunca derruba uma requisição.

## Clientes devem ramificar em `code` ou em `errorCode`?

Em `errorCode`. Esse é o identificador estável de tela (`ORDER_ALREADY_SHIPPED`, `VALIDATION`). `code` é a chave do catálogo usada para resolver a mensagem (`order.already_shipped`). Vários códigos podem compartilhar um error code. Nunca ramifique em `detail` — é texto traduzido do catálogo.

## Dá para usar o Offside sem ASP.NET Core?

Sim. `Offside` tem como alvo `netstandard2.0` e não depende de ASP.NET. Workers, CLIs e class libraries podem devolver `Result` e resolver mensagens com `IErrorMessageResolver`; só o mapeamento HTTP vive em `Offside.AspNetCore`.

## Por que não existe `ToActionResult(result, resolver, exposeExceptionDetails)` para o `Result` unitário?

Um descuido no conjunto de sobrecargas, mantido em vez de alterado enquanto a biblioteca é pré-1.0. O `Result<T>` genérico tem. Para um `Result` unitário, passe uma cultura explicitamente ou passe `null` pela sobrecarga com options para cair no `Accept-Language`.

## É seguro colocar dados do usuário em `Error.Arguments`?

Argumentos alimentam templates de mensagem, e mensagens vão para clientes. Identificadores e nomes de campo tudo bem; tokens, hashes de senha e connection strings não. Material de diagnóstico pertence a `Error.Unexpected(detail)`, que é sanitizado antes de sair na resposta.

## Por que a pasta do repositório se chama `DomainErrors`?

Histórico — o projeto foi renomeado para Offside. A solution, os pacotes e os namespaces são todos `Offside`; só um clone local pode ainda carregar o nome antigo.

## Está pronto para produção?

Está pré-1.0. Releases minor podem incluir mudanças que quebram; veja o [changelog](../../CHANGELOG.md). O comportamento documentado aqui é coberto por testes em `net8.0` e `net10.0`.
