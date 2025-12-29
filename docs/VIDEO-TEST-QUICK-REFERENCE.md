# 🎥 Video Test Infrastructure - Quick Reference

**Total Videos**: 140 | **Total Size**: 3.19 GB | **Total Duration**: 17.5 hours

---

## 🔗 Access Links

| Resource | URL/Path |
|----------|----------|
| **Interactive HTML** | `file:///home/mpasqui/insightlearn_WASM/InsightLearn_WASM/docs/VIDEO-TEST-LINKS.html` |
| **Markdown List** | [docs/VIDEO-TEST-LINKS.md](./VIDEO-TEST-LINKS.md) |
| **Summary** | [docs/VIDEO-TEST-INFRASTRUCTURE-SUMMARY.md](./VIDEO-TEST-INFRASTRUCTURE-SUMMARY.md) |
| **Grafana Dashboard** | http://localhost:3000/d/video-streaming-dashboard |
| **Log File** | `/var/log/insightlearn-video-check.log` |

---

## ⚡ Quick Commands

### Test Single Video

```bash
# Production URL
curl -I "https://www.insightlearn.cloud/api/video/stream/693bd380a633a1ccf7f519e7"

# Localhost URL
curl -I "http://localhost:31081/api/video/stream/693bd380a633a1ccf7f519e7"
```

### Run Full Verification

```bash
# Execute verification script
/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/scripts/verify-test-videos.sh

# Expected output: ✓ All test videos are accessible and functional
```

### Check Systemd Timer

```bash
# Status
systemctl status insightlearn-video-check.timer

# Next scheduled run
systemctl list-timers insightlearn-video-check.timer

# Manual run
sudo systemctl start insightlearn-video-check.service

# View logs
tail -f /var/log/insightlearn-video-check.log
```

### Open HTML Test Page

```bash
# Firefox
firefox /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/docs/VIDEO-TEST-LINKS.html

# Or serve via HTTP
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/docs
python3 -m http.server 8000
# Then open: http://localhost:8000/VIDEO-TEST-LINKS.html
```

### Access Grafana Dashboard

```bash
# Port-forward (if needed)
kubectl port-forward -n insightlearn svc/grafana 3000:3000 &

# Open dashboard
firefox "http://localhost:3000/d/video-streaming-dashboard"

# Or apply ConfigMap
kubectl apply -f k8s/31-grafana-video-streaming-dashboard.yaml
```

---

## 📋 Sample ObjectIds

```
693bd380a633a1ccf7f519e7  # Lesson 1 - Video (WebM, 1.03 MB)
693bdeada633a1ccf7f519f9  # About Bananas (MP4, 65.34 MB)
693be12aa633a1ccf7f51a3c  # Doctor in Industry (MP4, 111.84 MB)
693be1363312dba5e79987da  # Health: Your Posture (MP4, 63.44 MB)
693be13c3312dba5e799881b  # From the Ground Up (MP4, 113.58 MB)
```

---

## 🎯 Verification Checklist

```bash
# Quick verification - run all
ls -lh docs/VIDEO-TEST-LINKS.html && \
systemctl is-active insightlearn-video-check.timer && \
grep -q "Video Infrastructure Check" Jenkinsfile && \
ls -lh k8s/31-grafana-video-streaming-dashboard.yaml && \
echo "✅ All components verified"
```

---

## 📊 Statistics

- **Courses**: 11 test courses
- **MP4 Videos**: 130 (93%)
- **WebM Videos**: 10 (7%)
- **Largest**: 117.25 MB (Doctor in Industry)
- **Smallest**: 1.03 MB (WebM tests)

---

**Updated**: 2025-12-26
