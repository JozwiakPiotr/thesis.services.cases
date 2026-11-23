# 1. Stawianie klastra Single Node Openshift

## 1.1 Pobieranie oprogramowania

1. logujemy się na konsole `https://console.redhat.com/openshift/overview`
2. `create cluster`
3. domena AD `contoso.lab` więc **base domain** dla openshift musi być inne - `ocp.contoso.lab`
4. **Number of control plane nodes** ustawiamy na 1 (SNO)
5. **Host Discovery** podaje klucz publiczny ssh i pobieram obraz ISO
6. utworzyć VM i przydzielić już docelowe zasoby (32gb ram, )

# 2. Wstępna konfiguracja
## 2.1 Włączenie Developer Console
oc patch console.operator.openshift.io/cluster --type='merge' -p '{"spec":{"customization":{"perspectives":[{"id":"dev","visibility":{"state":"Enabled"}}]}}}'
