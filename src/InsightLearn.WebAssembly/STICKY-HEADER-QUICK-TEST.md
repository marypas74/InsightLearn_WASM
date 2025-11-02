# Quick Test - Sticky Header Fix

**5-Minute Testing Protocol**

---

## STEP 1: Visual Verification (Desktop)

```bash
# Apri browser
https://new.insightlearn.cloud

# Test rapido scroll
1. ✅ Scrolla pagina → Header rimane in cima?
2. ✅ Shadow diventa più marcata dopo scroll?
3. ✅ Apri dropdown "Categories" → Funziona?
```

---

## STEP 2: Console Verification

**Chrome DevTools → Console**:

```javascript
// Quick diagnostic
const header = document.querySelector('.main-header');
const tests = {
    'Position Sticky': getComputedStyle(header).position === 'sticky',
    'Z-index OK': parseInt(getComputedStyle(header).zIndex) >= 10001,
    'Backdrop Blur': getComputedStyle(header).backdropFilter.includes('blur'),
    'GPU Accelerated': getComputedStyle(header).transform.includes('matrix')
};

console.table(tests);
// ✅ Tutti TRUE = SUCCESSO
```

---

## STEP 3: Mobile Test (DevTools)

```bash
# DevTools → Toggle Device Toolbar (Ctrl+Shift+M)
1. iPhone 12 Pro
2. Scrolla → Header sticky?
3. ✅ Altezza compatta (56px)?
4. ✅ Touch targets grandi abbastanza?
```

---

## STEP 4: Performance Check

```bash
# DevTools → Lighthouse
1. Run audit (Desktop + Mobile)
2. ✅ Performance >90?
3. ✅ Accessibility 100?
```

---

## QUICK FIX se problemi

### Header non sticky:

```bash
# Hard refresh
Ctrl + Shift + R  (Windows/Linux)
Cmd + Shift + R   (Mac)

# Clear cache completamente
DevTools → Application → Clear Storage → Clear site data
```

### Shadow non cambia:

```javascript
// Console test
document.querySelector('.main-header').classList.add('scrolled');
// Shadow DEVE cambiare visivamente
```

---

## SUCCESS CRITERIA

✅ **PASS se**:
- Header visibile durante scroll
- Shadow dinamico funziona
- Dropdowns apertura corretta
- Performance Lighthouse >90
- Mobile responsive OK

❌ **FAIL se**:
- Header scompare durante scroll
- Console errors JavaScript
- Performance <80
- Mobile broken layout

---

**Next**: Se PASS → Deploy production
**If FAIL** → Vedi `STICKY-HEADER-SOLUTION.md` sezione Troubleshooting
