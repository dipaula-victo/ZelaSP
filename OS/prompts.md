# Registro de uso de IA generativa — ZelaSP

A IA generativa foi utilizada como apoio durante o desenvolvimento e a documentação do projeto. As respostas foram tratadas como orientação, e não como comprovação automática de funcionamento.

## Exemplos de solicitações utilizadas

1. **Provisionamento Linux:** orientação para estruturar diretórios, permissões e scripts de provisionamento no Ubuntu.  
   **Verificação:** execução dos scripts no CT-NODE, inspeção dos diretórios e validação de sintaxe com `bash -n`.

2. **Serviço systemd:** apoio para configurar o agente como serviço e executá-lo com usuário nominal diferente do administrador.  
   **Verificação:** `systemctl status`, `systemctl show`, `systemctl is-active`, `systemctl is-enabled` e inspeção dos processos.

3. **Monitoramento e logs:** apoio para revisar coleta de CPU, memória, disco e estado dos serviços, além do registro de eventos.  
   **Verificação:** leitura dos arquivos de log e testes de reinício, indisponibilidade de serviço e falha de escrita.

4. **Rede e DNS:** orientação para validar conectividade, SSH e resolução de nomes.  
   **Verificação:** testes de `ping`, conexão SSH, `dig`, `named-checkconf` e `named-checkzone`.

5. **Documentação:** apoio para organizar README, relatório técnico e evidências conforme os requisitos da entrega.  
   **Verificação:** conferência das informações documentadas com a configuração real das máquinas e as capturas de tela.

Nenhuma senha, chave privada ou credencial foi incluída neste registro.
