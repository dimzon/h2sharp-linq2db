# H2Sharp.Linq2Db

Copyright 2026 The H2Sharp contributors.

## What it depends on

* **H2Sharp** -- the embedded H2 port, <https://github.com/dimzon/h2sharp>. Dual-licensed under
  the MPL 2.0 and the EPL 1.0; see the engine's `LICENSE-H2.txt`. This provider is a separate
  work: it links the engine's public API and does not copy its code.
* **linq2db** -- the LINQ provider framework, MIT licensed, <https://github.com/linq2db/linq2db>.
  This provider is a separate package built against linq2db's public API.

## The strong name key

`H2Sharp.snk` in the repository root is the key this project signs its assemblies with, and it
is committed on purpose. A strong name is not a security feature -- it identifies a build, and
anyone can read the public key out of any assembly that carries it -- so the private key is not
a secret in the sense that would make committing it a leak.

It is committed because the key has to be the same key everywhere the assembly is built: the
engine opens its internals to a named friend, and a key that lived only in one place, or one
that each contributor replaced with their own, would break that arrangement.

## No warranty

EXCEPT AS SET OUT IN THE LICENSE, THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED.
