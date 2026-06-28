# Checklist manual no Unity Editor — Mobile/Touch + Localização

Tudo que está nesta lista **precisa ser feito uma vez no Unity Editor** (não é possível fazer por código fora do Editor). Siga na ordem.

## 1. Input System (controles touch + gamepad)

1. Abra `Assets/PlayerControls.inputactions` no Editor.
2. Confirme os novos bindings já presentes no arquivo (adicionados via código):
   - `Player/Interact` → `<Gamepad>/buttonSouth` (A no Xbox, Cross no PS5/PS4).
   - `Player/AimCursor` → `<Gamepad>/rightStick`.
3. Clique em **"Generate C# Class"** no Inspector do asset (regra do projeto: sempre regenerar após editar `.inputactions`).
4. Confirme que `PlayerControls.cs` foi atualizado (verifique a data de modificação).

## 2. Construir o Canvas de controles mobile/gamepad

1. Menu **Tools > Sowur Shield > Rebuild Mobile Controls UI**.
2. Isso cria `MobileControlsManager` (com `MobileControlsCanvas` filho, joystick virtual, botão de ação, e reticle de cursor de gamepad).
3. Arraste o GameObject `MobileControlsManager` para dentro da cena `Assets/Scenes/MainMenu.unity` (ele mesmo aplica `DontDestroyOnLoad` e sobrevive a todas as trocas de cena).
4. Salve a cena.

## 3. Testar mobile/touch

- **No navegador desktop**: abra a build WebGL, ative o "Device Toolbar" (Chrome/Edge DevTools, ícone de celular) para emular touch, recarregue a página.
- **Em celular real**: acesse a URL do GitHub Pages direto do navegador do celular.
- Verifique: joystick aparece no canto inferior esquerdo, botão de ação no canto inferior direito, ambos respeitam a Safe Area (não cortados por notch/barra do navegador), e o joystick move o personagem / botão interage.
- **No Editor** (Play Mode): controles touch não aparecem por padrão (detecção via `.jslib` só funciona em build WebGL real). Para testar a lógica de visibilidade no Editor, edite temporariamente `MobileDetector.ForceTouchInEditor = true` via algum script de debug, ou aguarde o teste em build real.

## 4. Testar gamepad (Xbox / PS5)

1. Conecte um controle Xbox ou PS5/PS4 (via USB ou Bluetooth) ao computador.
2. Em Play Mode (Editor) ou em build, abra `SampleScene`.
3. Teste:
   - Stick esquerdo / D-pad → move o personagem.
   - Botão A (Xbox) / Cross (PS5) → interage (`E` equivalente) e também funciona como "clique" do cursor virtual de ferramentas.
   - Stick direito → move um reticle na tela; aponte para um bloco de solo e pressione A/Cross para usar a ferramenta equipada (enxada, regador etc).
   - Botão de Sprint: `L3`/clique do stick esquerdo.
4. Caso o reticle não apareça: confirme que `GamepadCursorReticle` foi criado pelo builder (passo 2) e que `GamepadVirtualCursor.cursorVisual` está atribuído (deve estar, o builder já faz essa wiring via `SerializedObject`).

## 5. Localização — criar estrutura no Editor

**Agora é um único clique.** Não precisa abrir a janela de Localization Tables manualmente nem criar nada à mão:

1. Menu **Tools > Sowur Shield > Setup Localization (Full)**.
2. Isso cria automaticamente: os 3 Locales (en/pt/es), o asset `Localization Settings` (e já o ativa como o oficial do projeto), as 12 String Table Collections, e importa todas as ~226 traduções do CSV — tudo de uma vez.
3. Vai aparecer uma janela no final com um resumo (quantos Locales, tabelas e entradas foram criados). Se aparecer algum aviso no Console sobre tabela "skipped", rode o comando de novo (geralmente resolve, pode ser ordem de criação).
4. **Pode rodar esse comando quantas vezes quiser** — ele não duplica nada, só cria o que ainda não existe e atualiza as traduções.

Se só quiser re-importar o CSV depois de editá-lo (sem recriar Locales/Tables), use **Tools > Sowur Shield > Import Localization CSV** em vez do setup completo.

## 6. Ligar os campos `LocalizedString` automaticamente (sem Inspector manual)

Em vez de configurar ~220 campos um por um no Inspector, há agora um comando que faz isso direto via código:

1. Abra `Assets/Scenes/MainMenu.unity` (Editor, não Play Mode).
2. Menu **Tools > Sowur Shield > Auto-Wire Localized Fields**.
3. Isso varre TODOS os objetos das cenas abertas E todos os prefabs do projeto, lendo `Assets/Localization/field_map.json` (gerado a partir dos comentários `// table "X", key "y.z"` no código), e atribui a tabela/chave certa em cada campo `LocalizedString` encontrado.
4. Salve a cena (Ctrl+S).
5. **Repita o mesmo passo com `Assets/Scenes/SampleScene.unity` aberta** (e `CombatScene.unity` se existir) — o comando só enxerga objetos das cenas que estão abertas no momento, então rode de novo em cada cena pra cobrir os componentes que vivem só nelas (inventário, animais, combate, etc.). Os prefabs são cobertos automaticamente em qualquer execução.
6. Leia o resumo no final — ele informa quantos objetos/prefabs foram atualizados e quantos campos foram preenchidos. Se aparecer "Classes not found", normalmente é um componente que só existe instanciado em runtime (self-spawning UI) — esses já são cobertos pela busca de prefabs/AssetDatabase, mas se sobrar algum, rode de novo com o jogo em Play Mode parado num ponto em que esses objetos existam na Hierarchy, e adapte se necessário.

**Atenção**: essa abordagem escreve direto nos campos serializados via `SerializedObject` (o mesmo caminho que o picker do Inspector usa), então persiste corretamente no arquivo da cena/prefab — mas é uma forma não convencional de configurar Localization (não é o fluxo recomendado pela Unity). Se algum texto aparecer errado depois, você pode sempre corrigir manualmente aquele campo específico no Inspector.

## 7. Seletor de idioma no primeiro boot + dropdown em Settings

Também automatizado — não precisa criar painel/botões manualmente:

1. Com `Assets/Scenes/MainMenu.unity` aberta, menu **Tools > Sowur Shield > Build Language UI (in open MainMenu scene)**.
2. Isso cria automaticamente: o painel `LanguageSelectPanel` (com botões English/Português/Español) ao lado do `mainPanel`, e o dropdown "Language" dentro do `settingsPanel` existente — ambos já ligados nos campos do `MainMenuUI`.
3. Salve a cena (Ctrl+S).
4. Se quiser ajustar a posição/visual dos elementos criados (cores, tamanho, fonte), edite normalmente no Inspector/Scene View depois — o script só cuida da criação e da ligação dos campos, não da estética fina.

## 8. Testar a troca de idioma

1. Em Play Mode, abra o menu principal.
2. No primeiro boot (sem `PlayerPrefs["Language"]` setado — pode limpar via `Edit > Clear All PlayerPrefs` para simular), confirme que o painel de seleção de idioma aparece antes do menu principal.
3. Escolha um idioma, confirme que o menu principal aparece corretamente e os textos já migrados mudam de idioma.
4. Abra Settings, mude o dropdown de idioma, confirme troca em tempo real (sem precisar reiniciar).
5. Avance para `SampleScene` e confirme que os textos de gameplay (inventário, animais, combate, etc.) também aparecem traduzidos — isso só funcionará nos componentes onde o campo `LocalizedString` foi de fato configurado no Inspector (passo 6).

**Importante sobre "trocar idioma não muda nada visualmente"**: o sistema interno de Localização troca corretamente ao escolher um idioma, mas cada texto só é escrito na tela **uma vez**, quando o painel abre/o jogo carrega — não existe atualização automática e contínua de tudo que já está visível. Por isso só alguns componentes (`MainMenuUI`, `UIManagerPlayer` — relógio/dinheiro) foram conectados pra re-escrever seus textos quando o idioma muda (`LocalizationManager.OnLanguageChanged`). Para os demais, **fechar e reabrir o painel** (ex: fechar e abrir o inventário de novo) já mostra o texto no idioma novo, mesmo sem esse refresh automático — porque o painel reconstrói o texto do zero ao abrir.

Texto **estático de prefab** (botões fixos como "New Game", "Continue", "Settings") nunca muda de idioma sozinho — precisa do componente "Localize String Event" (passo 10), que é trabalho manual no Editor.

## 10. Textos estáticos em prefabs/cenas (fora do código C#)

Os passos acima cobrem **todo texto gerado dinamicamente por código**. Texto **estático** dentro de prefabs/cenas (labels fixos que nunca são escritos via `.text = ...` em C#, ex: títulos de botões do menu, headers fixos) precisa do seguinte processo manual, por item:

1. Selecione o GameObject com `TextMeshProUGUI`.
2. Adicione o componente **"Localize String Event"** (`Add Component > Localization > Localize String Event`).
3. No campo "String Reference", selecione a tabela e a chave (crie a chave em `UI_Common` ou na tabela apropriada se ainda não existir no CSV — adicione a linha no CSV e re-rode o importer, ou edite a tabela direto na janela de Localization Tables).
4. No evento `On Update String (String)`, arraste o próprio GameObject e selecione `TextMeshProUGUI.SetText(string)` como callback.

**Prioridade sugerida** (ordem de mais visível para menos visível):
1. Botões do menu principal (`New Game`, `Continue`, `Load Game`, `Settings`, `Credits`, `Quit`) em `mainPanel`.
2. Labels do `settingsPanel` ("Master Volume", "Music Volume", "SFX Volume", "Fullscreen", "Resolution", "Language").
3. Título/botões do `creditsPanel`.
4. Headers fixos de HUD em `SampleScene.unity` (ex: título "Inventory" se for um label estático de prefab, não escrito por código).
5. Labels fixos em `CombatScene.unity` (ex: cabeçalhos de painéis se forem estáticos).

## Observações finais

- Se usar `pt-BR` em vez de `pt` como código de locale, ajuste: o CSV (coluna `pt` → seria preciso renomear, mas o importer lê pelo nome do header, então basta trocar o header `pt` por `pt-BR` no CSV antes de importar), `LocalizationManager.cs` (códigos `"pt"` usados em `SetLanguage`), e `MainMenuUI.cs` (switch `1 => "pt"`).
- Dados de ScriptableObjects (itens, animais, conquistas, diálogos) **não foram migrados** — ficam em inglês/português conforme o asset original, pois são conteúdo de dados, não strings de código. Se quiser localizá-los também, é um trabalho separado (provavelmente adicionando campos `LocalizedString` aos próprios ScriptableObjects).
