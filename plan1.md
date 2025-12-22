# Piano di Sviluppo: Responsive Device Detection & Tab Resize System

**Versione Target**: 2.2.2-dev
**Data**: 2025-12-22
**Stato**: PRONTO PER ESECUZIONE AUTONOMA

---

## Analisi dello Stato Attuale

### Componenti GIÀ IMPLEMENTATI (v2.2.1-dev)

| Componente | File | Stato | Note |
|------------|------|-------|------|
| **DeviceDetectionService (C#)** | `Services/DeviceDetectionService.cs` | ✅ Completo | 197 linee, JSInterop, eventi viewport/orientation |
| **IDeviceDetectionService** | `Services/IDeviceDetectionService.cs` | ✅ Completo | 169 linee, interfaccia + DTOs (DeviceInfo, DeviceType, etc.) |
| **deviceDetection.js** | `wwwroot/js/deviceDetection.js` | ✅ Completo | 363 linee, detection UA + viewport, CSS classes auto |
| **mobile-optimizations.css** | `wwwroot/css/mobile-optimizations.css` | ✅ Completo | 251 linee, breakpoints 768px/1024px, touch targets 44px |
| **DI Registration** | `Program.cs:116` | ✅ Registrato | `AddScoped<IDeviceDetectionService, DeviceDetectionService>()` |
| **JS Include** | `index.html:809` | ✅ Incluso | `<script src="js/deviceDetection.js">` |
| **CSS Include** | `index.html:542` | ✅ Incluso | Deferred loading con `media="print" onload` |

### Componenti MANCANTI

| Componente | Descrizione | Priorità |
|------------|-------------|----------|
| **Utilizzo nei componenti Razor** | Nessun componente usa `IDeviceDetectionService` | ALTA |
| **Layout adattivo MainLayout** | Nessun rendering condizionale basato su device | ALTA |
| **Tab resize dinamico** | Tabs non si adattano automaticamente | ALTA |
| **Test automatici** | Nessun test di integrazione device detection | MEDIA |

---

## Piano di Implementazione

### FASE 1: Integrazione DeviceDetection nel MainLayout (30 min)

**Obiettivo**: Rendere il MainLayout responsivo in base al tipo di dispositivo rilevato.

**File da modificare**: `src/InsightLearn.WebAssembly/Layout/MainLayout.razor`

**Azioni**:
1. Iniettare `IDeviceDetectionService`
2. Aggiungere stato per `DeviceInfo` e `DeviceType`
3. Sottoscrivere eventi `ViewportChanged` e `OrientationChanged`
4. Applicare classi CSS condizionali basate su device type
5. Passare `DeviceType` ai componenti figli via `CascadingValue`

**Codice da aggiungere**:
```csharp
@inject IDeviceDetectionService DeviceDetection

@code {
    private DeviceInfo? _deviceInfo;
    private DeviceType _deviceType = DeviceType.Desktop;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _deviceInfo = await DeviceDetection.GetDeviceInfoAsync();
            _deviceType = _deviceInfo.DeviceType;
            DeviceDetection.ViewportChanged += OnViewportChanged;
            StateHasChanged();
        }
    }

    private void OnViewportChanged(object? sender, ViewportChangedEventArgs e)
    {
        _deviceType = e.NewWidth switch
        {
            < 768 => DeviceType.Mobile,
            < 1024 => DeviceType.Tablet,
            _ => DeviceType.Desktop
        };
        InvokeAsync(StateHasChanged);
    }
}
```

---

### FASE 2: Creare Componente ResponsiveTabs (45 min)

**Obiettivo**: Creare un componente tabs che si adatta automaticamente al dispositivo.

**File da creare**: `src/InsightLearn.WebAssembly/Components/ResponsiveTabs.razor`

**Comportamento**:
- **Desktop (≥1024px)**: Tabs orizzontali standard
- **Tablet (768-1023px)**: Tabs orizzontali con icone + testo compatto
- **Mobile (<768px)**: Tabs come bottom navigation o dropdown

**Struttura**:
```razor
@inject IDeviceDetectionService DeviceDetection

<CascadingValue Value="@_deviceType" Name="DeviceType">
    @if (_deviceType == DeviceType.Mobile)
    {
        <!-- Mobile: Bottom sheet o dropdown -->
        <div class="tabs-mobile">
            @MobileTabsContent
        </div>
    }
    else if (_deviceType == DeviceType.Tablet)
    {
        <!-- Tablet: Tabs compatte con scroll orizzontale -->
        <div class="tabs-tablet">
            @TabletTabsContent
        </div>
    }
    else
    {
        <!-- Desktop: Tabs standard -->
        <div class="tabs-desktop">
            @DesktopTabsContent
        </div>
    }
</CascadingValue>
```

---

### FASE 3: Aggiornare CSS per Tab Responsive (30 min)

**File da modificare**: `src/InsightLearn.WebAssembly/wwwroot/css/mobile-optimizations.css`

**Aggiunte CSS**:
```css
/* ========== RESPONSIVE TABS ========== */

/* Desktop Tabs */
.tabs-desktop {
    display: flex;
    flex-wrap: nowrap;
    gap: 8px;
    border-bottom: 2px solid var(--border-color, #e5e7eb);
}

.tabs-desktop .tab-item {
    padding: 12px 24px;
    font-size: 14px;
    font-weight: 500;
    white-space: nowrap;
}

/* Tablet Tabs */
@media (min-width: 768px) and (max-width: 1023px) {
    .tabs-tablet {
        display: flex;
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
        scrollbar-width: none;
        padding-bottom: 2px;
    }

    .tabs-tablet::-webkit-scrollbar { display: none; }

    .tabs-tablet .tab-item {
        flex-shrink: 0;
        padding: 10px 16px;
        font-size: 13px;
    }

    .tabs-tablet .tab-item .tab-text {
        display: none; /* Solo icone su tablet stretto */
    }

    @media (min-width: 900px) {
        .tabs-tablet .tab-item .tab-text { display: inline; }
    }
}

/* Mobile Tabs - Bottom Navigation Style */
@media (max-width: 767px) {
    .tabs-mobile {
        position: fixed;
        bottom: calc(56px + var(--mobile-safe-area-bottom, 0px));
        left: 0;
        right: 0;
        background: var(--bg-primary, #fff);
        border-top: 1px solid var(--border-color, #e5e7eb);
        display: flex;
        justify-content: space-around;
        z-index: 8999;
        padding: 8px 0;
    }

    .tabs-mobile .tab-item {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 8px 12px;
        font-size: 11px;
        color: var(--text-secondary, #6b7280);
        min-width: var(--touch-target-min, 44px);
        min-height: var(--touch-target-min, 44px);
    }

    .tabs-mobile .tab-item.active {
        color: var(--primary-color, #a435f0);
    }

    .tabs-mobile .tab-item i {
        font-size: 20px;
        margin-bottom: 4px;
    }
}
```

---

### FASE 4: Aggiornare Learning Space Tabs (30 min)

**File da modificare**: `src/InsightLearn.WebAssembly/Pages/LearningSpace.razor` (o componente tabs esistente)

**Obiettivo**: Applicare il sistema responsive ai tabs del Learning Space (Overview, Notebook, Transcript, Q&A).

**Azioni**:
1. Iniettare `IDeviceDetectionService`
2. Condizionare il rendering dei tabs in base a `DeviceType`
3. Su mobile: trasformare tabs in bottom sheet o collapsible accordion

---

### FASE 5: Test e Validazione (20 min)

**Test manuali da eseguire**:
1. Aprire Chrome DevTools → Toggle Device Toolbar
2. Testare viewport: 375px (iPhone), 768px (iPad), 1440px (Desktop)
3. Verificare CSS classes applicate: `device-mobile`, `device-tablet`, `device-desktop`
4. Verificare resize dinamico cambiando dimensione finestra
5. Verificare orientation change (portrait/landscape)

**Script di test automatico** (opzionale):
```bash
# Test CSS classes in browser console
document.body.classList.contains('device-mobile')
document.body.classList.contains('device-tablet')
document.body.classList.contains('device-desktop')

# Test viewport detection
window.deviceDetection.getDeviceInfo()
```

---

### FASE 6: Build e Deploy (15 min)

**⚠️ IMPORTANTE - REGOLA VERSIONING**

Prima di ogni build, INCREMENTARE la versione in `Directory.Build.props`:
```xml
<VersionPrefix>2.2.2</VersionPrefix>  <!-- DA 2.2.1 → 2.2.2 -->
```

**Comandi di Build e Deploy**:

```bash
# 1. INCREMENTARE VERSIONE (OBBLIGATORIO!)
sed -i 's/<VersionPrefix>2.2.1</<VersionPrefix>2.2.2</g' Directory.Build.props

# 2. Build locale per verifica
dotnet build src/InsightLearn.WebAssembly/InsightLearn.WebAssembly.csproj -c Release

# 3. Build immagine Docker WASM
podman build -f Dockerfile.wasm -t localhost/insightlearn/wasm:2.2.2-dev .

# 4. Export e import in K3s
podman save localhost/insightlearn/wasm:2.2.2-dev -o /tmp/wasm.tar
echo 'PASSWORD' | sudo -S /usr/local/bin/k3s ctr images import /tmp/wasm.tar

# 5. Deploy con kubectl set image
kubectl set image deployment/insightlearn-wasm-blazor-webassembly -n insightlearn \
    wasm-blazor=localhost/insightlearn/wasm:2.2.2-dev

# 6. Verifica rollout
kubectl rollout status deployment/insightlearn-wasm-blazor-webassembly -n insightlearn --timeout=180s

# 7. Verifica pods
kubectl get pods -n insightlearn -l app=insightlearn-wasm-blazor-webassembly
```

---

## Checklist di Completamento

- [ ] **FASE 1**: MainLayout integrato con DeviceDetectionService
- [ ] **FASE 2**: ResponsiveTabs component creato
- [ ] **FASE 3**: CSS responsive tabs aggiunto
- [ ] **FASE 4**: Learning Space tabs adattato
- [ ] **FASE 5**: Test manuali completati
- [ ] **FASE 6**: Build e deploy eseguiti

---

## Riepilogo File da Modificare/Creare

| Azione | File | Linee Stimate |
|--------|------|---------------|
| MODIFICA | `Layout/MainLayout.razor` | +50 linee |
| CREA | `Components/ResponsiveTabs.razor` | ~150 linee |
| CREA | `Components/ResponsiveTabs.razor.cs` | ~100 linee |
| MODIFICA | `wwwroot/css/mobile-optimizations.css` | +80 linee |
| MODIFICA | `Pages/LearningSpace.razor` (se esiste) | +30 linee |
| MODIFICA | `Directory.Build.props` | 1 linea (versione) |

**Totale stimato**: ~410 linee di codice

---

## Note Importanti

1. **DeviceDetectionService è già completo** - Non serve riscriverlo, solo usarlo nei componenti
2. **CSS classes sono auto-applicate** - `deviceDetection.js` aggiunge automaticamente `device-mobile`, `device-tablet`, `device-desktop` al `<body>`
3. **Breakpoints standard**:
   - Mobile: `< 768px`
   - Tablet: `768px - 1023px`
   - Desktop: `≥ 1024px`
4. **Touch targets**: Minimo 44x44px per accessibilità (già configurato in CSS)
5. **Events disponibili**: `ViewportChanged` e `OrientationChanged` per reattività in tempo reale

---

## Esecuzione Autonoma

Questo piano è progettato per essere eseguito in modo **completamente autonomo** da Claude Code.

**Comando per avviare l'esecuzione**:
> "Esegui il piano in plan1.md dalla FASE 1 alla FASE 6, incluso build e rollout"

**Tempo stimato totale**: ~2.5 ore
