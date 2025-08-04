
![landscape picture, fuzzy hedgehog with cyan colored fur, walking on a sunny patch of the woods while carrying a bag of money. Dollar bills are flying everywhere, being dropped from the bag. FLUX.1-schnell](./banner.png)

# Rinha de Backend 2025 - Payment processor

Minha submissão para a [Rinha de Backend 2025](https://github.com/zanfranceschi/rinha-de-backend-2025), que esse ano tem como tema **Payment processor**.

## Tecnologias Utilizadas
- [FSharp](https://fsharp.org)
- [SQLite](https://www.sqlite.org/index.html)
- [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

## Arquitetura

Eu decidi seguir o caminho mais simples possível utilizando soluções nativas em F#, a aplicação em si é um monolito.

A API REST é feita com o [Falco Framework](https://www.falcoframework.com). Um controller simples faz o parsing da requisição e chama um servio de Gateway que internamente define uma fila pra processar as transações usando o pattern [MailboxProcessor](https://fsharpforfunandprofit.com/posts/concurrency-actor-model/).

O Gateway tem referência pra duas instâncias do Client que efetua as requisições para os gatways de pagamento usando a biblioteca [FsHttp](https://github.com/fsprojects/FsHttp). Cada Client mantém uma fila interna que faz polling pro healthcheck dos gateways, e essa informação é usada pelo Gateway para decidir qual cliente usar. 

Caso a requisição falhe, o Gateway enfileira ela novamente para ser tentada novamente. Em caso de sucesso o serivço de Persistence salva a transação em uma tabela no SQLite usando [Donald](https://github.com/pimbrouwers/Donald).

![Arquitetura](./architecture.jpeg)

## Autor

Eu trabalho com desenvolvimento desde 2006, atualmente atuo como Senior Software Engineer e trabalho principalmente com Java. Já usei diversas linguagens de programação, mas a minha favorita ainda é F#.


<div>
  <a href="https://www.linkedin.com/in/victorhogemann/"><img src="https://img.shields.io/badge/linkedin-0077B5.svg?style=for-the-badge&logo=linkedin&logoColor=white"></a>
  <a href="https://bsky.app/profile/victor.hogemann.com">
    <img src="https://img.shields.io/badge/bluesky-1DA1F2.svg?style=for-the-badge&logo=bluesky&logoColor=white">
  <a href="https://github.com/vhogemann">
    <img src="https://img.shields.io/badge/github-333333.svg?style=for-the-badge&logo=github&logoColor=white"></a>
</div>