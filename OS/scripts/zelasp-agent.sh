#!/bin/bash
# Agente de monitoramento ZelaSP

BASE="/srv/citizentech"
LOG="$BASE/logs/agent.log"
RM="558447"
EXECUTOR="$(whoami)"
HOST="$(hostname)"
INTERVAL=15

log_message() {
    local LEVEL="$1"
    local MESSAGE="$2"
    local ENTRY
    ENTRY="$(date '+%Y-%m-%d %H:%M:%S') | RM=$RM | USER=$EXECUTOR | HOST=$HOST | LEVEL=$LEVEL | $MESSAGE"

    if ! echo "$ENTRY" >> "$LOG" 2>/dev/null; then
        logger -t zelasp-agent "$ENTRY"
        echo "$ENTRY" >&2
    fi
}

get_cpu_usage() {
    local CPU1 IDLE1 CPU2 IDLE2 TOTAL1 TOTAL2
    read -r _ USER NICE SYSTEM IDLE IOWAIT IRQ SOFTIRQ STEAL _ < /proc/stat
    IDLE1=$((IDLE + IOWAIT))
    TOTAL1=$((USER + NICE + SYSTEM + IDLE + IOWAIT + IRQ + SOFTIRQ + STEAL))
    sleep 1
    read -r _ USER NICE SYSTEM IDLE IOWAIT IRQ SOFTIRQ STEAL _ < /proc/stat
    IDLE2=$((IDLE + IOWAIT))
    TOTAL2=$((USER + NICE + SYSTEM + IDLE + IOWAIT + IRQ + SOFTIRQ + STEAL))
    echo $(( (100 * ((TOTAL2 - TOTAL1) - (IDLE2 - IDLE1))) / (TOTAL2 - TOTAL1 + 1) ))
}

get_memory_usage() {
    awk '/MemTotal/ {total=$2} /MemAvailable/ {available=$2}
         END {if (total > 0) printf "%d", (100 * (total-available) / total); else print 0}' /proc/meminfo
}

get_disk_usage() {
    df -P "$BASE" | awk 'NR==2 {gsub("%","",$5); print $5}'
}

log_message "INFO" "Agente iniciado"

while true; do
    CPU="$(get_cpu_usage)"
    MEMORY="$(get_memory_usage)"
    DISK="$(get_disk_usage)"

    log_message "INFO" "CPU=${CPU}% MEMORY=${MEMORY}% DISK=${DISK}%"

    if [ "$CPU" -gt 80 ]; then
        log_message "ALERT" "Uso de CPU acima de 80%: ${CPU}%"
    fi
    if [ "$MEMORY" -gt 85 ]; then
        log_message "ALERT" "Uso de memoria acima de 85%: ${MEMORY}%"
    fi
    if [ "$DISK" -gt 80 ]; then
        log_message "ALERT" "Uso de disco acima de 80%: ${DISK}%"
    fi

    for SERVICE in bind9 ssh; do
        if ! systemctl is-active --quiet "$SERVICE"; then
            log_message "ALERT" "Servico $SERVICE esta inativo"
        fi
    done

    sleep "$INTERVAL"
done
