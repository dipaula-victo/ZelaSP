# CitizenTech — ZelaSP

**Global Solution 2026.2 — Operating Systems**  
**Turma:** 3ESPH  
**ODS:** 11 — Cidades e Comunidades Sustentáveis (e apoio às ODS 9 e 10)

## Ficha da CitizenTech

| Campo | Conteúdo |
|---|---|
| **Nome da CitizenTech** | ZelaSP |
| **Cidade e bairro** | São Paulo / Jabaquara (distrito) |
| **Cidadão beneficiado** | Morador, pedestre ou comerciante do Jabaquara que convive diariamente com pontos viciados de descarte irregular, calçadas bloqueadas e lixeiras transbordando, exposto ao mau cheiro e a vetores de doenças enquanto aguarda o serviço de limpeza urbana. |
| **Problema, em uma frase** | Moradores do Jabaquara não têm um jeito simples de avisar e acompanhar lixeiras cheias e descarte irregular, e os problemas ficam dias sem resposta visível. |
| **Como funciona** | O morador registra a ocorrência com foto pelo app e uma lixeira com sensor reporta o nível automaticamente. A API guarda os dados, atualiza o status e mantém o histórico, enquanto a infraestrutura Linux sustenta o monitoramento operacional, resolução de nomes e auditoria. |
| **Papel de Operating Systems** | Infraestrutura Linux independente (`CT-NODE` e `CT-CLIENT`) com provisionamento automatizado, armazenamento dedicado, agente de monitoramento gerenciado via `systemd`, auditoria, SSH e servidor DNS autoritativo BIND9. |
| **Tecnologias** | Ubuntu Linux, Bash Shell Script, `systemd`, BIND9, OpenSSH, VirtualBox (rede interna) |
| **Ecossistema** | Moradores, comerciantes locais, subprefeitura e limpeza urbana, dados abertos SP156 |
| **Indicador de impacto (proposto)** | Tempo médio entre o registro e a resolução da ocorrência, em dias (sem medição real nesta etapa). |
| **ODS conectados** | **ODS 11** (Meta 11.6 — gestão de resíduos municipais e espaços públicos salubres), **ODS 9** (infraestrutura resiliente e inovação) e **ODS 10** (acesso igualitário ao acompanhamento de solicitações). |
| **Observação e escuta** | Baseado em relatos públicos do CADES e Conselho Participativo do Jabaquara (2025–2026) que apontam 101 pontos viciados de descarte irregular. Não houve entrevista direta com moradores, utilizando-se apenas dados públicos secundários. |
| **Fonte dos dados** | SP156 (base do 3º trimestre de 2026, dicionário de dados de 2020 em diante) para contextualização do território; métricas operacionais e hosts simulados em ambiente controlado de laboratório. |
| **Repositório** | https://github.com/dipaula-victo/ZelaSP |

---

## Equipe

| Integrante | RM | Usuário nominal | Responsabilidade |
|---|---:|---|---|
| Felipe Paes de Barros Muller Carioba | 558447 | `Felipe_RM558447` | Líder do grupo e administrador |
| Djalma Moreira de Andrade Filho | 555530 | `Djalma_RM555530` | Executor do agente ZelaSP |
| Lucas Rodrigues de Queiroz | 556323 | `Lucas_RM556323` | Integrante |
| Victor Hugo de Paula | 554787 | `Victor_RM554787` | Integrante |

## Proposta

O ZelaSP é uma infraestrutura Linux de monitoramento e sustentação operacional projetada para apoiar a identificação de indisponibilidades e condições anormais em serviços de uma cidade inteligente. Em alinhamento ao escopo independente da disciplina (enunciado v05), o ambiente utiliza duas máquinas virtuais Ubuntu em rede interna isolada, com armazenamento persistente dedicado, agente de monitoramento executado como serviço tolerante a falhas, registros de auditoria, conectividade remota via SSH e resolução de nomes interna via DNS (BIND9).

## Arquitetura

| Máquina | Hostname | IP da rede interna | Função |
|---|---|---|---|
| CT-NODE | `ct-node-558447` | `192.168.56.10/24` | Servidor, armazenamento, agente, SSH e DNS |
| CT-CLIENT | `ct-client-558447` | `192.168.56.20/24` | Cliente para testes de rede, SSH e DNS |

Rede interna VirtualBox: `intnet-zelasp` (`192.168.56.0/24`). As VMs também contam com interface NAT para acesso externo e instalação de pacotes.

## Usuários e permissões

Os quatro integrantes são representados por contas nominais idênticas nas duas VMs, vinculadas ao grupo operacional `zelasp`. A administração do sistema é realizada pelo usuário `Felipe_RM558447`, enquanto o serviço do agente executa sob o princípio do menor privilégio com o usuário `Djalma_RM555530` (grupo `zelasp`), sem privilégios de `root`.

## Armazenamento

O `CT-NODE` utiliza um segundo disco virtual de 10 GB, particionado e formatado em `ext4` com rótulo `ZELASP_DATA`, montado de forma persistente em `/srv/citizentech` via `/etc/fstab` (identificado por UUID). O volume contém os diretórios operacionais `bin/`, `config/`, `data/` e `logs/`, configurados com propriedade do grupo `zelasp` e permissões colaborativas.

## Scripts e serviço

- `scripts/01-provision-infra.sh` — prepara a estrutura de diretórios em `/srv/citizentech`, valida permissões, armazenamento e condições básicas da infraestrutura.
- `scripts/02-provision-agent.sh` — instala o agente, prepara os arquivos de log e valida as dependências operacionais.
- `scripts/zelasp-agent.sh` — coleta periodicamente indicadores de **CPU**, **memória RAM**, **uso de disco** e estado dos serviços `bind9` e `ssh`, disparando alertas quando limites críticos são atingidos.
- `configs/zelasp-agent.service` — unidade `systemd` configurada com `User=Djalma_RM555530`, `Group=zelasp` e `Restart=always` para recuperação automática em caso de falha.

O agente registra informações de identificação (`RM`, `USER` e `HOST`) e eventos de coleta/alerta em `/srv/citizentech/logs/agent.log`. As etapas de provisionamento e auditoria são registradas em `/srv/citizentech/logs/audit.log`.

## Rede e DNS

- **CT-NODE:** `192.168.56.10/24`
- **CT-CLIENT:** `192.168.56.20/24`
- **Acesso remoto:** OpenSSH ativo no `CT-NODE` para administração e testes a partir do `CT-CLIENT`.
- **Zona DNS direta:** `zelasp.test` (`configs/db.zelasp.test`)
- **Zona DNS reversa:** `56.168.192.in-addr.arpa` (`configs/db.192.168.56`)
- **Registros principais:** `ns1`, `ct-node`, `api` e `status` apontam para `192.168.56.10`; `ct-client` aponta para `192.168.56.20`.

## Link para o Vídeo no YouTube

https://youtu.be/flA3pD1Umrc

## Estrutura da entrega

```text
.
├── README.md
├── Relatorio_Tecnico_ZelaSP_D03.pdf
├── prompts.md
├── configs/
│   ├── db.192.168.56
│   ├── db.zelasp.test
│   ├── fstab
│   ├── named.conf.local
│   ├── named.conf.options
│   └── zelasp-agent.service
├── scripts/
│   ├── 01-provision-infra.sh
│   ├── 02-provision-agent.sh
│   └── zelasp-agent.sh
└── Evidencias/
    ├── E01_01_identificacao_client.png
    ├── E01_02_identificacao_node.png
    ├── E02_01_usuarios_grupos_node.png
    ├── E02_02_usuarios_grupos_client.png
    ├── E03_01_diretorios_permissoes.png
    ├── E04_01_lsblk.png
    ├── E04_02_blkid_findmnt.png
    ├── E04_03_fstab.png
    ├── E04_04_df.png
    ├── E05_01_status_agente.png
    ├── E05_02_usuario_processo_agente.png
    ├── E05_03_log_agente.png
    ├── E06_01_audit_log.png
    ├── E06_02_alertas.png
    ├── E06_03_falha_escrita.png
    ├── E07_01_interfaces_rotas_node.png
    ├── E07_03_interfaces_rotas_client.png
    ├── E08_01_ping_node.png
    ├── E08_02_ssh_node.png
    ├── E09_01_dns_ns1.png
    ├── E09_02_dns_api.png
    ├── E09_03_dns_status.png
    └── E09_05_named_checkzone.png