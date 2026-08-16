# Mensagens e culturas

*[English](../messages.md) · [Voltar às docs](README.md)*

O texto dos erros fica em catálogos JSON, um por cultura. O C# guarda os metadados — código, kind, argumentos — e nunca a redação.

## Formato do catálogo

Um objeto JSON plano mapeando código de erro para template de mensagem:

```json
{
  "not_found": "{resource} '{id}' não foi encontrado.",
  "conflict": "Conflito em {resource}.",
  "validation": "{field} é inválido.",
  "unexpected": "Ocorreu um erro inesperado."
}
```

Os onze códigos padrão — `not_found`, `gone`, `conflict`, `validation`, `bad_request`, `unauthorized`, `forbidden`, `precondition_failed`, `unprocessable`, `too_many_requests`, `unexpected` — cobrem todas as factories nativas. Adicione uma entrada por código customizado que você introduzir com `Error.Custom`.

## Registro

```csharp
builder.Services.AddOffside(options =>
{
    options.AddJson(CultureInfo.InvariantCulture, File.ReadAllText("errors/errors.json"));
    options.AddJson(new CultureInfo("pt-BR"),     File.ReadAllText("errors/errors.pt-BR.json"));
    options.AddJson(new CultureInfo("es"),        File.ReadAllText("errors/errors.es.json"));
});
```

`AddJson` recebe o **conteúdo** do catálogo, não um caminho. Há também uma sobrecarga com `Stream` para recursos embutidos:

```csharp
options.AddJson(CultureInfo.InvariantCulture,
    typeof(Program).Assembly.GetManifestResourceStream("MyApp.errors.json")!);
```

Os catálogos são parseados uma vez, no registro. Um arquivo malformado falha na inicialização, não na primeira requisição que der erro.

**Um catálogo de cultura invariante é obrigatório.** Sem ele, `AddOffside` lança `InvalidOperationException`. Ele é o fallback final, então todo código deveria aparecer nele.

## Fallback de cultura

A resolução tenta três buscas em ordem e para na primeira que acertar:

1. A cultura exata — `pt-BR`
2. O pai dela — `pt`
3. O catálogo invariante

Assim um catálogo `pt` serve tanto `pt-BR` quanto `pt-PT`, e você traduz os específicos só onde a redação realmente difere. Se nenhum catálogo define o código, o resolver devolve **o próprio código** — a resposta continua bem formada e a entrada faltante fica visível em vez de vir em branco.

Em um host ASP.NET Core, a cultura vem do header `Accept-Language` a menos que você passe uma explicitamente. Veja [Culturas](aspnet-guide.md#culturas).

## Interpolação

Os tokens do template são `{name}`, preenchidos a partir de `Error.Arguments`:

```csharp
Error.NotFound("order", 42)
// argumentos: resource = "order", id = 42
```

```json
{ "not_found": "{resource} '{id}' não foi encontrado." }
```

```
order '42' não foi encontrado.
```

Três comportamentos a conhecer:

- **Um token sem argumento correspondente permanece literal.** `Error.NotFound("order")` contra o template acima produz `order '{id}' não foi encontrado.` — um argumento nulo é pulado, não zerado. Visível, não silencioso.
- **Os valores são formatados com `InvariantCulture`.** Números e datas saem estáveis independentemente da cultura da requisição. Formate você mesmo antes de passar, se precisar de saída sensível à localidade.
- **Isto é substituição de token, não `string.Format`.** Não existe `{0}`, nem especificadores de formato como `{amount:C}`, nem escaping — chaves sem argumento correspondente sobrevivem como escritas.

## Adicionando um idioma

1. Copie `errors/errors.json` para `errors/errors.<cultura>.json`.
2. Traduza os valores. Deixe as chaves e os `{tokens}` intactos.
3. Registre com `options.AddJson(new CultureInfo("<cultura>"), ...)`.

```json
{
  "not_found": "{resource} '{id}' was not found.",
  "conflict": "Conflict on {resource}.",
  "validation": "{field} is invalid.",
  "unexpected": "An unexpected error occurred."
}
```

Um catálogo traduzido não precisa estar completo. O que ele omitir cai para a cultura pai e depois para o catálogo invariante, então dá para publicar uma tradução parcial e completá-la com o tempo.

`offside init` escreve um catálogo em inglês e um em português do Brasil como ponto de partida — veja a [página do CLI](cli.md).

## Resolvers customizados

`AddOffside` registra um `JsonErrorMessageResolver` como o singleton `IErrorMessageResolver`. Para buscar mensagens de outro lugar — um banco, assemblies satélite de recursos, um serviço de tradução — implemente a interface e registre-a no lugar:

```csharp
public sealed class ResxErrorMessageResolver : IErrorMessageResolver
{
    public string GetMessage(Error error, CultureInfo culture) =>
        Messages.ResourceManager.GetString(error.Code, culture) ?? error.Code;
}

builder.Services.AddSingleton<IErrorMessageResolver, ResxErrorMessageResolver>();
```

Nesse caso não chame `AddOffside` — ele registraria o resolver JSON ao lado do seu. Devolver `error.Code` para um código desconhecido é a convenção que vale manter: degrada para algo diagnosticável em vez de uma string vazia.
