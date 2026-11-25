# Arquivos de dados com índices parciais

Programa de console em C# que guarda dados de um e-commerce (produtos e sessões de usuários) em **arquivos binários com registros de tamanho fixo**, ordenados pela chave, e permite pesquisar neles direto no arquivo ou usando um **índice parcial**.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (o projeto usa `net10.0`)
- Um CSV de eventos de e-commerce com as colunas:

  ```
  event_time,event_type,product_id,category_id,category_code,brand,price,user_id,user_session
  ```

## Configuração

Os caminhos dos arquivos de dados estão fixos em [Program.cs](Trabalho01/Program.cs). Antes de rodar, troque-os por caminhos válidos na sua máquina:

```csharp
using var productsTableReader = new ProductsTableReader("C:\\caminho\\products.bin");
using var userSessionTableReader = new UserSessionsTableReader("C:\\caminho\\user_sessions.bin");
```

Os arquivos `.bin` são criados automaticamente se não existirem. O índice parcial de cada tabela é salvo ao lado, com extensão `.idx`.

## Compilar e rodar

Na raiz do repositório:

```bash
dotnet build Trabalho01.slnx
```

```bash
dotnet run --project Trabalho01/ECommerceDatabase.csproj
```

Também dá para abrir `Trabalho01.slnx` no Visual Studio e rodar com F5.

## Uso

O menu principal tem duas tabelas: **Produtos** e **Sessões dos usuários**. Cada uma oferece:

| Opção | O que faz |
|---|---|
| 1 - Inserir dados iniciais | Lê o CSV e substitui todo o conteúdo da tabela |
| 2 - Mostrar dados | Lista os registros paginados (10 por página) |
| 3 - Pesquisa direto no arquivo de dados | Busca binária no próprio arquivo `.bin` |
| 4 - Pesquisa usando índice parcial | Busca binária no `.idx` e leitura sequencial a partir dali |
| 5 - Inserir registro | Insere um novo registro mantendo a ordem da chave |
| 6 - Remover registro | Marca o registro como removido (remoção lógica) |

Na importação:

- **Produtos**: cada produto aparece em vários eventos; fica só a primeira ocorrência, e os produtos são ordenados por `product_id`.
- **Sessões**: o CSV **precisa já estar ordenado por `user_session`**; linhas sem sessão são ignoradas.

## Como funciona

### Arquivo de dados (`.bin`)

Cada tabela é um arquivo com registros de tamanho fixo, gravados em binário e ordenados pela chave:

- **Produto** (chave `product_id`, 100 bytes): id, id da categoria, código da categoria (38), marca (32), preço.
- **Sessão** (chave `user_session`, 70 bytes): id da sessão (36), id do usuário, id do produto, tipo do evento (8), data/hora.

Todo registro termina com três campos de controle:

- **flags** (1 byte): indica se o registro foi removido ou se é um registro de extensão;
- **continuation** (8 bytes): posição do próximo registro na ordem da chave (`0` = o próximo é o registro seguinte no arquivo);
- **`\n`** (1 byte), só para facilitar a visualização do arquivo.

O arquivo é dividido em duas áreas:

1. **Área principal**: os registros importados, em ordem e contíguos.
2. **Área de extensão**: registros inseridos depois, sempre gravados no fim do arquivo.

Uma inserção não move os registros existentes: o novo registro vai para o fim do arquivo e é "encadeado" ajustando o `continuation` do registro anterior a ele. Assim, percorrer a tabela seguindo os `continuation` sempre retorna os registros em ordem.

A remoção é lógica: só liga a flag de removido, e o registro passa a ser ignorado nas listagens e pesquisas.

### Pesquisa direto no arquivo

Faz uma busca binária na área principal para achar o último registro com chave menor que a procurada e, a partir dele, segue a cadeia de `continuation` até encontrar a chave (ou passar dela).

### Pesquisa com índice parcial (`.idx`)

O índice guarda a chave e a posição de **um a cada N registros** da área principal (N = 50 para produtos, 1000 para sessões). A busca binária é feita no índice, que fica em memória, e depois o arquivo de dados é lido a partir da posição encontrada, assim como na pesquisa direta.

Como inserções não alteram a área principal, o índice só é reconstruído quando o tamanho dela muda (por exemplo, após uma nova importação).

## Estrutura do projeto

- `TableReaders/` — classe base `TableReader` com toda a lógica de leitura, gravação, busca e índice.
- `Products/` e `UserSessions/` — modelo, serialização, menu e formulários de cada tabela.
- `Imports/` — leitura do CSV de eventos.
- `Menus/`, `Forms/`, `TeableViewers/`, `Consoles/` — utilitários da interface de console.
