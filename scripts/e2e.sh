#!/usr/bin/env bash
# Roteiro de ponta a ponta do ExpenseHub.
#
# Sobe a API com banco SQLite e senhas temporários, percorre os casos de docs/REQUISITOS.md e
# docs/MATRIZ-AUTORIZACAO.md comparando o status HTTP esperado com o obtido e desliga a API no fim.
# Não usa nem altera o banco de desenvolvimento.
#
# Requisitos: bash, curl e .NET SDK 10. No Windows, rode pelo Git Bash.
# Uso, a partir da raiz do repositório:
#   bash scripts/e2e.sh
# Sai com código 0 quando todas as verificações passam e 1 quando alguma falha.

set -u

PORT="${E2E_PORT:-5299}"
B="http://localhost:$PORT"
J='Content-Type: application/json'
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="$(mktemp -d)"
DB="$WORK/e2e.db"
API_DIR="$ROOT/sources/ExpenseHub.Api"
if command -v cygpath >/dev/null 2>&1; then DB="$(cygpath -m "$DB")"; API_DIR="$(cygpath -m "$API_DIR")"; fi

# Senhas geradas a cada execução, dentro da política padrão do Identity.
STAMP="$(date +%s)"
PW="Adm-${STAMP}!Aa1"
UPW="Usr-${STAMP}!Bb2"
WRONG_PW="Errada-${STAMP}!Cc3"

echo "Compilando..."
dotnet build "$ROOT/sources/ExpenseHub.slnx" -nologo -v quiet >/dev/null || { echo "Falha no build."; exit 1; }

echo "Iniciando a API em $B (banco temporário em $WORK)..."
ASPNETCORE_URLS="$B" \
ConnectionStrings__ExpenseHub="Data Source=$DB" \
Seed__AdminPassword="$PW" \
dotnet "$API_DIR/bin/Debug/net10.0/ExpenseHub.Api.dll" --contentRoot "$API_DIR" >"$WORK/api.log" 2>&1 &
API_PID=$!
trap 'kill "$API_PID" 2>/dev/null; wait "$API_PID" 2>/dev/null; rm -rf "$WORK"' EXIT

for _ in $(seq 1 60); do
  curl -s -o /dev/null "$B/health" && break
  sleep 1
done
curl -s -o /dev/null "$B/health" || { echo "A API não respondeu em $B. Log:"; cat "$WORK/api.log"; exit 1; }

PASS=0; FAIL=0
st() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
check() { # check "descrição" esperado obtido
  if [ "$2" = "$3" ]; then PASS=$((PASS+1)); printf "  ok    %-78s %s\n" "$1" "$3";
  else FAIL=$((FAIL+1)); printf "  FALHA %-78s esperado %s, obtido %s\n" "$1" "$2" "$3"; fi; }
tok() { curl -s -X POST $B/login -H "$J" -d "{\"email\":\"$1\",\"password\":\"$2\"}" | sed -E 's/.*"accessToken":"([^"]+)".*/\1/'; }
au() { echo "Authorization: Bearer $1"; }

ADM=$(tok admin@expensehub.local "$PW")
for u in ana beto bia carlos fin aud semrole; do curl -s -o /dev/null -X POST $B/register -H "$J" -d "{\"email\":\"$u@empresa.com\",\"password\":\"$UPW\"}"; done
USERS=$(curl -s $B/api/admin/users -H "$(au $ADM)")
uid() { echo "$USERS" | sed -E "s/.*\"id\":\"([^\"]+)\",\"email\":\"$1@empresa.com\".*/\1/"; }
setr() { st -X PUT $B/api/admin/users/$(uid $1)/roles -H "$J" -H "$(au $ADM)" -d "{\"roles\":$2}"; }
setr ana '["Employee"]' >/dev/null; setr beto '["Employee"]' >/dev/null; setr bia '["Employee","Approver"]' >/dev/null
setr carlos '["Approver"]' >/dev/null; setr fin '["Employee","Finance"]' >/dev/null; setr aud '["Auditor"]' >/dev/null
ANA=$(tok ana@empresa.com "$UPW"); BETO=$(tok beto@empresa.com "$UPW"); BIA=$(tok bia@empresa.com "$UPW"); CAR=$(tok carlos@empresa.com "$UPW")
FIN=$(tok fin@empresa.com "$UPW"); AUD=$(tok aud@empresa.com "$UPW"); SEM=$(tok semrole@empresa.com "$UPW")
BODY='{"description":"Taxi do aeroporto","amount":85.50,"expenseDate":"2026-09-20"}'
mk() { curl -s -X POST $B/api/expenses -H "$J" -H "$(au $1)" -d "$BODY" | sed -E 's/.*"id":"([^"]+)".*/\1/'; }
REJ='{"justification":"Nota fiscal ilegivel"}'

echo "== Cadastro, login e roles (I02, I03)"
check "cadastro com roles e recusado" 400 "$(st -X POST $B/register -H "$J" -d "{\"email\":\"x@empresa.com\",\"password\":\"$UPW\",\"roles\":[\"Admin\"]}")"
check "login com senha errada" 401 "$(st -X POST $B/login -H "$J" -d '{"email":"ana@empresa.com","password":"'"$WRONG_PW"'"}')"
check "listar usuarios sem token" 401 "$(st $B/api/admin/users)"
check "listar usuarios como Employee" 403 "$(st $B/api/admin/users -H "$(au $ANA)")"
check "role desconhecida" 400 "$(setr semrole '["SuperUser"]')"
ADMID=$(echo "$USERS" | sed -E 's/.*"id":"([^"]+)","email":"admin@expensehub.local".*/\1/')
check "Admin removendo a propria role Admin" 400 "$(st -X PUT $B/api/admin/users/$ADMID/roles -H "$J" -H "$(au $ADM)" -d '{"roles":["Employee"]}')"

echo "== Casos negativos obrigatorios da MATRIZ"
E_ANA=$(mk $ANA)
check "anonimo em rota protegida (criar)" 401 "$(st -X POST $B/api/expenses -H "$J" -d "$BODY")"
check "anonimo em rota protegida (listar)" 401 "$(st $B/api/expenses)"
check "autenticado sem role cria reembolso" 403 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $SEM)" -d "$BODY")"
check "autenticado sem role lista reembolsos" 403 "$(st $B/api/expenses -H "$(au $SEM)")"
check "Employee consulta reembolso de outro Employee" 404 "$(st $B/api/expenses/$E_ANA -H "$(au $BETO)")"
check "Employee edita reembolso de outro Employee" 404 "$(st -X PUT $B/api/expenses/$E_ANA -H "$J" -H "$(au $BETO)" -d "$BODY")"
check "Employee envia reembolso de outro Employee" 404 "$(st -X POST $B/api/expenses/$E_ANA/submit -H "$(au $BETO)")"
check "Employee consulta historico de outro Employee" 404 "$(st $B/api/expenses/$E_ANA/history -H "$(au $BETO)")"
check "listagem do beto nao traz o reembolso da ana" 0 "$(curl -s $B/api/expenses -H "$(au $BETO)" | grep -c "$E_ANA")"
check "Employee informa ownerId" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2026-09-20","ownerId":"outro"}')"
check "Employee altera ownerId na edicao" 400 "$(st -X PUT $B/api/expenses/$E_ANA -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2026-09-20","ownerId":"outro"}')"
check "usuario informa estado" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2026-09-20","status":"Approved"}')"
check "usuario informa horario" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2026-09-20","createdAtUtc":"2026-01-01T00:00:00Z"}')"
check "usuario informa ator na reprovacao" 400 "$(st -X POST $B/api/expenses/$E_ANA/reject -H "$J" -H "$(au $CAR)" -d '{"justification":"Nota fiscal ilegivel","actorId":"x"}')"
E_BIA=$(mk $BIA); st -X POST $B/api/expenses/$E_BIA/submit -H "$(au $BIA)" >/dev/null
check "Approver aprova o proprio reembolso" 403 "$(st -X POST $B/api/expenses/$E_BIA/approve -H "$(au $BIA)")"
check "Approver reprova o proprio reembolso" 403 "$(st -X POST $B/api/expenses/$E_BIA/reject -H "$J" -H "$(au $BIA)" -d "$REJ")"
E_FIN=$(mk $FIN); st -X POST $B/api/expenses/$E_FIN/submit -H "$(au $FIN)" >/dev/null; st -X POST $B/api/expenses/$E_FIN/approve -H "$(au $CAR)" >/dev/null
check "Finance paga o proprio reembolso" 403 "$(st -X POST $B/api/expenses/$E_FIN/pay -H "$(au $FIN)")"
check "Auditor cria reembolso" 403 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $AUD)" -d "$BODY")"
check "Auditor edita reembolso" 403 "$(st -X PUT $B/api/expenses/$E_ANA -H "$J" -H "$(au $AUD)" -d "$BODY")"
check "Auditor envia reembolso" 403 "$(st -X POST $B/api/expenses/$E_ANA/submit -H "$(au $AUD)")"
check "Auditor aprova reembolso" 403 "$(st -X POST $B/api/expenses/$E_BIA/approve -H "$(au $AUD)")"
check "Auditor paga reembolso" 403 "$(st -X POST $B/api/expenses/$E_FIN/pay -H "$(au $AUD)")"
check "Admin lista reembolsos" 403 "$(st $B/api/expenses -H "$(au $ADM)")"
check "Admin cria reembolso" 403 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ADM)" -d "$BODY")"
check "Admin aprova reembolso" 403 "$(st -X POST $B/api/expenses/$E_BIA/approve -H "$(au $ADM)")"
check "Admin consulta historico" 403 "$(st $B/api/expenses/$E_ANA/history -H "$(au $ADM)")"
check "Approver consulta rascunho de outra pessoa" 404 "$(st $B/api/expenses/$E_ANA -H "$(au $CAR)")"
check "Finance consulta reembolso Submitted" 404 "$(st $B/api/expenses/$E_BIA -H "$(au $FIN)")"
check "id inexistente" 404 "$(st $B/api/expenses/00000000-0000-0000-0000-000000000001 -H "$(au $AUD)")"

echo "== Validacoes (I04)"
check "descricao com 9 caracteres" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"123456789","amount":10,"expenseDate":"2026-09-20"}')"
check "valor 0" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":0,"expenseDate":"2026-09-20"}')"
check "valor 0.01" 201 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":0.01,"expenseDate":"2026-09-20"}')"
check "valor acima de Int32.MaxValue" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":2147483647.01,"expenseDate":"2026-09-20"}')"
check "data futura" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2099-01-01"}')"
check "categoria inexistente" 400 "$(st -X POST $B/api/expenses -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto","amount":10,"expenseDate":"2026-09-20","categoryId":99}')"

echo "== Fluxo completo e estados (I04 a I08)"
E=$(mk $ANA)
check "edicao do proprio rascunho" 200 "$(st -X PUT $B/api/expenses/$E -H "$J" -H "$(au $ANA)" -d '{"description":"Taxi do aeroporto ida e volta","amount":171,"expenseDate":"2026-09-20","categoryId":1}')"
check "Approver aprova rascunho (ainda nao enviado)" 404 "$(st -X POST $B/api/expenses/$E/approve -H "$(au $CAR)")"
check "envio" 200 "$(st -X POST $B/api/expenses/$E/submit -H "$(au $ANA)")"
check "envio repetido" 409 "$(st -X POST $B/api/expenses/$E/submit -H "$(au $ANA)")"
check "edicao fora de Draft" 409 "$(st -X PUT $B/api/expenses/$E -H "$J" -H "$(au $ANA)" -d "$BODY")"
check "pagamento de Submitted" 409 "$(st -X POST $B/api/expenses/$E/pay -H "$(au $FIN)")"
check "Approver ve o Submitted" 200 "$(st $B/api/expenses/$E -H "$(au $CAR)")"
check "aprovacao" 200 "$(st -X POST $B/api/expenses/$E/approve -H "$(au $CAR)")"
check "aprovacao repetida" 409 "$(st -X POST $B/api/expenses/$E/approve -H "$(au $CAR)")"
check "reprovacao de Approved" 409 "$(st -X POST $B/api/expenses/$E/reject -H "$J" -H "$(au $CAR)" -d "$REJ")"
check "Finance ve o Approved" 200 "$(st $B/api/expenses/$E -H "$(au $FIN)")"
check "Approver nao ve mais o Approved" 404 "$(st $B/api/expenses/$E -H "$(au $CAR)")"
check "pagamento" 200 "$(st -X POST $B/api/expenses/$E/pay -H "$(au $FIN)")"
check "pagamento repetido" 409 "$(st -X POST $B/api/expenses/$E/pay -H "$(au $FIN)")"
check "aprovacao de Paid" 409 "$(st -X POST $B/api/expenses/$E/approve -H "$(au $CAR)")"
H=$(curl -s $B/api/expenses/$E/history -H "$(au $ANA)")
check "historico: acoes na ordem" "Created Updated Submitted Approved Paid" "$(echo "$H" | grep -oE '"action":"[A-Za-z]+"' | sed -E 's/"action":"([A-Za-z]+)"/\1/' | tr '\n' ' ' | sed 's/ $//')"
check "historico: edicao registra as alteracoes" 1 "$(echo "$H" | grep -c 'amount: 85.50 -> 171.00')"
check "historico: instantes em UTC (terminam em Z)" 5 "$(echo "$H" | grep -oE '"occurredAtUtc":"[^"]+Z"' | wc -l | tr -d ' ')"
check "Finance consulta historico do Paid" 200 "$(st $B/api/expenses/$E/history -H "$(au $FIN)")"
check "Auditor consulta historico" 200 "$(st $B/api/expenses/$E/history -H "$(au $AUD)")"
check "Approver nao consulta historico do Paid" 404 "$(st $B/api/expenses/$E/history -H "$(au $CAR)")"
R=$(mk $ANA); st -X POST $B/api/expenses/$R/submit -H "$(au $ANA)" >/dev/null
check "reprovacao sem justificativa" 400 "$(st -X POST $B/api/expenses/$R/reject -H "$J" -H "$(au $CAR)" -d '{}')"
check "reprovacao com justificativa curta" 400 "$(st -X POST $B/api/expenses/$R/reject -H "$J" -H "$(au $CAR)" -d '{"justification":"curta"}')"
check "reprovacao" 200 "$(st -X POST $B/api/expenses/$R/reject -H "$J" -H "$(au $CAR)" -d "$REJ")"
check "reprovacao repetida" 409 "$(st -X POST $B/api/expenses/$R/reject -H "$J" -H "$(au $CAR)" -d "$REJ")"
check "pagamento de Rejected" 409 "$(st -X POST $B/api/expenses/$R/pay -H "$(au $FIN)")"
check "edicao de Rejected (final)" 409 "$(st -X PUT $B/api/expenses/$R -H "$J" -H "$(au $ANA)" -d "$BODY")"
check "historico guarda a justificativa" 1 "$(curl -s $B/api/expenses/$R/history -H "$(au $AUD)" | grep -c 'Nota fiscal ilegivel')"

echo "== Listagem por perfil (filtro no banco)"
check "Auditor lista todos (inclusive rascunho da ana)" 1 "$(curl -s $B/api/expenses -H "$(au $AUD)" | grep -c "$E_ANA")"
check "Approver lista so Submitted" 0 "$(curl -s $B/api/expenses -H "$(au $CAR)" | grep -oE '"status":"[A-Za-z]+"' | grep -vc Submitted)"
check "Finance lista so Approved e Paid" 0 "$(curl -s $B/api/expenses -H "$(au $FIN)" | grep -oE '"status":"[A-Za-z]+"' | grep -vcE 'Approved|Paid' )"
check "Employee lista so os proprios" 0 "$(curl -s $B/api/expenses -H "$(au $BETO)" | grep -oE '"id":"' | wc -l | tr -d ' ')"

echo "== Troca de roles exige novo login (I03)"
setr beto '["Employee","Auditor"]' >/dev/null
check "token antigo apos troca de roles" 401 "$(st $B/api/expenses -H "$(au $BETO)")"
BETO2=$(tok beto@empresa.com "$UPW")
check "token novo enxerga como Auditor" 200 "$(st $B/api/expenses/$E_ANA -H "$(au $BETO2)")"

echo
echo "RESULTADO: $PASS ok, $FAIL falhas"
[ "$FAIL" -eq 0 ]
