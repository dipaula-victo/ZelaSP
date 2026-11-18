# Amostras fictícias

PNG/JPEG são cópias binárias das constantes de `backend/tests/ZelaSP.Tests/ImagemFixtures.cs`: pixel magenta 1×1 gerado localmente por System.Drawing/Bitmap durante a TASK-003, sem fotos reais/downloads. Créditos: fixture própria do projeto ZelaSP, implementação assistida por IA. Não houve geração de imagem ou execução da fixture C# nesta fase; apenas extração das constantes Base64 para arquivos e cálculo local de comprimento/hash.

| Arquivo | MIME previsto | Bytes | SHA-256 de referência |
|---|---|---:|---|
| pixel-ficticio.png | image/png | 120 | a5c8f1a1213865e9ef89420f46e08085aa9e1c94a1723bf77218d44865f10575 |
| pixel-ficticio.jpeg | image/jpeg | 634 | 9d8e6351c67193ebe9c11de358c8e54f127521aa511d6a1dc6003f64877e4860 |

Origem/validade histórica das fixtures documentada na TASK-003. Copiar/hash não é novo teste de upload, decodificação/renderização ou persistência. Para exibição, ampliar no visualizador sem salvar/converter os bytes. `relato-pii-protegido.esperado.txt` é somente expectativa manual, não saída capturada do detector.

Relato foi elaborado como exemplo de teste, sem identificar uma pessoa: CPF algorítmico, formato de telefone e domínio example.test, senha declaradamente fictícia e termo de saúde sem pessoa/contexto clínico. Não são credenciais de qualquer sistema. Nunca substituir por dados reais. Limites e revisão estática em `../PII_REVISAO_ESTATICA.md`.

Ressalva: um CPF com dígitos verificadores válidos não é necessariamente inexistente ou exclusivamente fictício. Não foi verificada sua titularidade, nem se o telefone está em uso. Não associar esses números a pessoas, nem consultar serviços externos para identificar titulares. A finalidade de teste não constitui comprovação de inexistência.
