# CLI

*[English](../cli.md) · [Voltar às docs](README.md)*

`Offside.Tool` gera catálogos de erro e instala skills de agente para que assistentes de código trabalhando no seu repositório conheçam as convenções do Offside.

## Instalar

```bash
dotnet tool install -g Offside.Tool
```

A ferramenta tem como alvo `net8.0` e faz roll-forward para majors posteriores, então uma máquina com apenas .NET 10 instalado consegue executá-la.

## Uso

```
offside init [--dir <caminho>] [--force]
```

| Opção | Efeito |
|---|---|
| `--dir <caminho>` | Raiz do projeto de destino. Padrão: diretório atual. |
| `--force` | Sobrescreve arquivos existentes. Sem ela, arquivos existentes são preservados. |

`offside --help` imprime o mesmo resumo.

## O que ele escreve

Quatro skills, em cada um dos três diretórios de agente:

```
.cursor/skills/offside-setup/    .agents/skills/offside-setup/    .claude/skills/offside-setup/
.cursor/skills/offside-domain/   .agents/skills/offside-domain/   .claude/skills/offside-domain/
.cursor/skills/offside-aspnet/   .agents/skills/offside-aspnet/   .claude/skills/offside-aspnet/
.cursor/skills/offside-mediatr/  .agents/skills/offside-mediatr/  .claude/skills/offside-mediatr/
```

| Skill | Cobre |
|---|---|
| `offside-setup` | Integrar o Offside a um projeto existente: pacotes, catálogos, DI, camadas |
| `offside-domain` | Factories, `Custom`, regras de `Result`, escape hatch |
| `offside-aspnet` | Mapeamento de endpoints, formato da resposta, severidade, sanitização de 500 |
| `offside-mediatr` | Registro, publicação ordenada de notificações, coleta scoped e retries |

Mais dois templates de catálogo:

```
errors/errors.json         invariante (inglês)
errors/errors.pt-BR.json   português do Brasil
```

Todo caminho escrito é ecoado no stdout, seguido dos comandos de pacote a rodar em seguida.

## Segurança

Sem `--force`, arquivos existentes são **pulados, nunca sobrescritos** — rodar `offside init` duas vezes é inofensivo, e não vai destruir um catálogo que você já traduziu. `--force` sobrescreve tudo que de outra forma seria pulado, então só use quando quiser as versões originais de volta.

O comando sai com `1` e escreve no stderr quando as skills não são encontradas ou o destino não pode ser escrito; caso contrário, `0`.

## Sem o CLI

Nada aqui é obrigatório. A ferramenta é uma conveniência — você pode escrever `errors/errors.json` à mão a partir do template em [Mensagens e culturas](messages.md) e dispensar as skills inteiramente.
