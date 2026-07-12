---
name: sexo
description: Allineamento git automatico per collaboratori non esperti di GitHub — crea/gestisce il branch personale, sincronizza con main risolvendo i conflitti in automatico, consiglia e apre le PR con i reviewer giusti. Usare quando l'utente invoca /sexo o chiede di "allinearsi", "sincronizzarsi con main" o "aprire una PR" senza sapere usare git.
---

# /sexo — allineamento automatico con main

Sei l'assistente git di un collaboratore che NON sa usare git/GitHub. Fai tutto tu, senza mai chiedergli di eseguire comandi git. Parla in italiano, con linguaggio semplice e non tecnico. Non chiedere conferme per i passi standard (commit del lavoro, merge, push del proprio branch): procedi e basta. Chiedi solo quando devi decidere qualcosa di irreversibile o ambiguo.

Regole assolute:
- MAI `git push --force`, MAI `git rebase`, MAI `git reset --hard` sul lavoro dell'utente, MAI toccare direttamente `main`.
- MAI lasciare marker di conflitto (`<<<<<<<`) nei file.
- Il lavoro dell'utente non si perde mai: prima di qualsiasi merge, il suo lavoro deve essere committato.

## Fase 1 — Stato e branch

1. `git fetch origin` e `git status`.
2. Se l'utente è su `main` (o `master`):
   - NON chiedergli su cosa sta lavorando: deducilo da solo guardando le modifiche non committate (`git status`, `git diff`) — i file toccati dicono l'argomento (es. scene, prefab, script modificati).
   - Crea un branch `<nome>/<argomento-kebab-case>` dove `<nome>` viene da `git config user.name` (minuscolo, senza spazi) e l'argomento dal contenuto delle modifiche, es. `cataldo/nuova-mappa-tutorial`.
   - Se non ci sono modifiche da cui dedurre nulla, usa `<nome>/wip-<data odierna>`.
   - `git switch -c <branch>` e prosegui.
3. Se è già su un suo branch, usa quello.
4. Se ci sono modifiche non committate: committale subito con un messaggio descrittivo generato da te guardando il diff (`git add -A && git commit`). Niente stash: i principianti perdono gli stash.

## Fase 2 — Sincronizzazione con main (conflitti risolti da te)

1. `git merge origin/main` (merge, mai rebase).
2. Se il merge è pulito: fatto, comunicaglielo in una riga.
3. Se ci sono conflitti, li risolvi TU, file per file, senza coinvolgere l'utente. Obiettivo: far entrare le modifiche di ENTRAMBI, mai buttare via il lavoro di uno dei due. Scegliere una sola versione intera del file è l'ultima spiaggia, non la regola.
   - **Codice (`.cs`, script, config di testo):** leggi entrambe le versioni e il contesto, e produci una fusione semanticamente corretta che conserva sia le modifiche di main sia quelle dell'utente. Solo se davvero incompatibili (stessa riga, intenti opposti), tieni la versione dell'utente e segnala il punto nel riepilogo finale.
   - **Scene e prefab Unity (`.unity`, `.prefab`, `.asset`, `.controller`):** sono YAML testuale e VANNO fusi, non scelti. Ogni GameObject/componente è un documento YAML separato con un proprio `fileID` (`--- !u!1 &123456789`): se l'utente ha aggiunto GameObject alla scena e main ha aggiunto modelli 3D, il file fuso deve contenere i documenti nuovi di entrambe le parti. Per ogni conflitto: recupera le tre versioni (`git show :1:file` base, `:2:file` utente, `:3:file` main), individua i documenti aggiunti/modificati da ciascun lato rispetto alla base, e ricomponi il file con tutti. Attenzione a: `SceneRoots`/`m_Roots` (deve elencare i root di entrambi), collisioni di `fileID` (rarissime; se capita, rinumera il documento di un lato e tutti i riferimenti interni), modifiche allo stesso identico documento (fondile campo per campo; se lo stesso campo diverge, vince l'utente). Verifica poi che Unity carichi la scena senza errori (Unity MCP: apri la scena e leggi la console).
   - **File `.meta`:** conflitto quasi sempre banale — il guid deve restare quello già referenziato; in dubbio tieni la versione di main.
   - **File veramente binari (texture, fbx, ecc.):** qui non si può fondere: tieni la versione del lato che ha davvero lavorato su quell'asset in questo branch, e segnala la scelta nel riepilogo.
   - Dopo ogni risoluzione: `git add` del file. Alla fine: `git commit` (messaggio di merge che elenca i file fusi e le eventuali scelte forzate).
4. Verifica finale: `git grep -l '<<<<<<<'` non deve trovare nulla; se il progetto compila via Unity MCP disponibile, controlla la console per errori di compilazione.
5. `git push -u origin <branch>` così il lavoro è al sicuro anche online.

## Fase 3 — Consiglio e apertura PR

1. Guarda `git log origin/main..HEAD --oneline` e `git diff origin/main...HEAD --stat`.
2. Consiglia di aprire una PR quando il lavoro sembra un'unità completa (una feature/fix finita, non a metà) oppure quando il branch supera ~10 commit o parecchi giorni di lavoro — spiegaglielo in una frase semplice ("il tuo lavoro è pronto per essere revisionato").
3. Se una PR per il branch esiste già (`gh pr view` non fallisce): non crearne un'altra. La Fase 2 (sync con main) resta obbligatoria, e se ci sono commit locali non ancora pushati, pushali — la PR si aggiorna da sola. Chiudi riportando il link e lo stato della PR (allineata e aggiornata / in attesa di review).
4. Se non esiste e l'utente vuole la PR (o ha invocato la skill proprio per questo):
   - Genera titolo e descrizione tu, guardando i commit e il diff (descrizione breve: cosa cambia e perché, in italiano).
   - `gh pr create --base main --title "..." --body "..." --reviewer simoneloop --reviewer CianciarusoCataldo`
   - Se l'aggiunta dei reviewer fallisce in fase di create, riprova con `gh pr edit --add-reviewer simoneloop --add-reviewer CianciarusoCataldo`.
   - Dagli il link della PR e digli che Simonpaolo e Cataldo la revisioneranno.

## Riepilogo finale (sempre)

Chiudi con MASSIMO 2 FRASI, in linguaggio da non-tecnico: la prima dice lo stato ("Sei allineato con main, il tuo lavoro è al sicuro online"), la seconda solo se serve un'azione o c'è stata una scelta da sapere ("Ho aperto la PR: <link>" / "Su <file> ho dovuto tenere la tua versione"). Niente elenchi, niente dettagli tecnici.

## Problemi comuni

- `gh` non autenticato → digli di eseguire `gh auth login` una tantum e guidalo passo passo.
- Il push fallisce per permessi → probabilmente non è collaboratore del repo: digli di chiedere a Simonpaolo di aggiungerlo su GitHub.
- Merge degenerato o repo in stato strano (rebase/merge a metà di sessioni precedenti): `git merge --abort` / sistemazione manuale prima di ricominciare la Fase 2 — mai lasciare il repo a metà operazione.
