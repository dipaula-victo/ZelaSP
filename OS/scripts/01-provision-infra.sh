#!/bin/bash
# Provisionamento da infraestrutura ZelaSP

set -u

BASE="/srv/citizentech"
LOG="$BASE/logs/audit.log"
EXECUTOR="${SUDO_USER:-$(id -un)}"

log_action() {
    local STATUS="$1"
    local ACTION="$2"
    local TIMESTAMP
    TIMESTAMP="$(date '+%Y-%m-%d %H:%M:%S')"

    if [ -d "$BASE/logs" ] && [ -w "$LOG" ]; then
        echo "$TIMESTAMP | USER=$EXECUTOR | STATUS=$STATUS | ACTION=$ACTION" >> "$LOG"
    else
        echo "$TIMESTAMP | USER=$EXECUTOR | STATUS=$STATUS | ACTION=$ACTION" >&2
        logger -t zelasp-provision "$STATUS: $ACTION"
    fi
}

if [ "$EUID" -ne 0 ]; then
    echo "Execute este script com sudo."
    exit 1
fi

mkdir -p "$BASE"/{bin,config,data,logs} || {
    echo "Erro ao criar a estrutura de diretorios."
    exit 1
}

touch "$LOG"
chown -R root:zelasp "$BASE"
chmod 2775 "$BASE" "$BASE"/{bin,config,data,logs}

log_action "STARTED" "Provisionamento da infraestrutura iniciado"

if ! mountpoint -q "$BASE"; then
    log_action "ERROR" "$BASE nao esta montado como volume dedicado"
    echo "ERRO: $BASE nao esta montado."
    exit 1
fi

if ! ip -4 addr show enp0s8 | grep -q "192.168.56.10/24"; then
    log_action "ERROR" "IP interno esperado nao encontrado em enp0s8"
    echo "ERRO: IP 192.168.56.10/24 nao encontrado em enp0s8."
    exit 1
fi

for SERVICE in ssh bind9; do
    if ! systemctl is-active --quiet "$SERVICE"; then
        log_action "ERROR" "Servico $SERVICE nao esta ativo"
        echo "ERRO: servico $SERVICE nao esta ativo."
        exit 1
    fi
done

log_action "SUCCESS" "Diretorios, montagem, IP e servicos validados"
log_action "COMPLETED" "Provisionamento da infraestrutura concluido"
echo "Provisionamento da infraestrutura concluido com sucesso!"
