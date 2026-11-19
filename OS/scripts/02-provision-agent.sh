#!/bin/bash
# Preparacao do ambiente do agente ZelaSP

set -u

BASE="/srv/citizentech"
LOG="$BASE/logs/audit.log"
AGENT_LOG="$BASE/logs/agent.log"
EXECUTOR="${SUDO_USER:-$(id -un)}"

log_action() {
    local STATUS="$1"
    local ACTION="$2"
    echo "$(date '+%Y-%m-%d %H:%M:%S') | USER=$EXECUTOR | STATUS=$STATUS | ACTION=$ACTION" >> "$LOG"
}

if [ "$EUID" -ne 0 ]; then
    echo "Execute este script com sudo."
    exit 1
fi

if ! mountpoint -q "$BASE"; then
    echo "ERRO: $BASE nao esta montado."
    exit 1
fi

mkdir -p "$BASE/logs"
touch "$AGENT_LOG"
chown root:zelasp "$AGENT_LOG"
chmod 664 "$AGENT_LOG"

for COMMAND in bash awk df free systemctl top; do
    if ! command -v "$COMMAND" >/dev/null 2>&1; then
        log_action "ERROR" "Dependencia ausente: $COMMAND"
        echo "ERRO: comando necessario nao encontrado: $COMMAND"
        exit 1
    fi
done

for SERVICE in bind9 ssh; do
    if ! systemctl list-unit-files | grep -q "^${SERVICE}\.service"; then
        log_action "ERROR" "Unidade systemd ausente: $SERVICE"
        echo "ERRO: unidade $SERVICE.service nao encontrada."
        exit 1
    fi
done

log_action "SUCCESS" "Arquivo de log, dependencias e servicos verificados"
echo "Provisionamento do agente concluido com sucesso!"
