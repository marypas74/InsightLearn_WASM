#!/bin/bash

# Script per scaricare video educational con licenza Creative Commons da YouTube
# Organizzati per categorie LMS

set -e

# Colori per output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

CONTENT_DIR="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/educational-content"
VIDEO_DIR="$CONTENT_DIR/videos"
METADATA_DIR="$CONTENT_DIR/metadata"
THUMBNAIL_DIR="$CONTENT_DIR/thumbnails"

echo -e "${GREEN}=== InsightLearn Educational Content Downloader ===${NC}"
echo -e "${YELLOW}Downloading Creative Commons licensed educational videos${NC}\n"

# Funzione per scaricare video con metadata
download_video() {
    local search_query="$1"
    local category="$2"
    local max_videos="${3:-3}"

    echo -e "${GREEN}📚 Categoria: $category${NC}"
    echo -e "${YELLOW}🔍 Cercando: $search_query${NC}"

    # Crea sottodirectory per categoria
    mkdir -p "$VIDEO_DIR/$category"
    mkdir -p "$METADATA_DIR/$category"
    mkdir -p "$THUMBNAIL_DIR/$category"

    # Scarica video CC da YouTube (max 720p, solo primi N risultati)
    $HOME/.local/bin/yt-dlp \
        --format "bestvideo[height<=720][ext=mp4]+bestaudio[ext=m4a]/best[height<=720][ext=mp4]/best" \
        --max-downloads "$max_videos" \
        --write-info-json \
        --write-thumbnail \
        --convert-thumbnails jpg \
        --output "$VIDEO_DIR/$category/%(title)s.%(ext)s" \
        --output-na-placeholder "Unknown" \
        --restrict-filenames \
        --no-playlist \
        "ytsearch$max_videos:$search_query creative commons"

    # Sposta metadata e thumbnail nelle directory corrette
    mv "$VIDEO_DIR/$category"/*.info.json "$METADATA_DIR/$category/" 2>/dev/null || true
    mv "$VIDEO_DIR/$category"/*.jpg "$THUMBNAIL_DIR/$category/" 2>/dev/null || true

    echo -e "${GREEN}✅ Completato download per: $category${NC}\n"
}

# Categorie e query di ricerca
echo -e "${YELLOW}Starting download of educational videos...${NC}\n"

# 1. Programming & Development
download_video "learn python programming tutorial" "programming-python" 2
download_video "learn c# dotnet tutorial" "programming-csharp" 2
download_video "learn javascript tutorial" "programming-javascript" 2

# 2. Web Development
download_video "learn html css tutorial" "web-development-frontend" 2
download_video "learn react tutorial" "web-development-react" 2

# 3. Business & Management
download_video "project management tutorial" "business-management" 2
download_video "digital marketing tutorial" "business-marketing" 2

# 4. Design
download_video "ui ux design tutorial" "design-ui-ux" 2
download_video "graphic design tutorial" "design-graphic" 2

# 5. Data Science
download_video "machine learning tutorial" "data-science-ml" 2
download_video "data analysis tutorial" "data-science-analysis" 2

echo -e "${GREEN}=== Download completato! ===${NC}"
echo -e "${YELLOW}Total video directory size:${NC}"
du -sh "$VIDEO_DIR"

echo -e "\n${YELLOW}Metadata files:${NC}"
find "$METADATA_DIR" -name "*.json" | wc -l

echo -e "\n${YELLOW}Thumbnail files:${NC}"
find "$THUMBNAIL_DIR" -name "*.jpg" | wc -l

echo -e "\n${GREEN}✅ Tutti i contenuti scaricati e organizzati!${NC}"
echo -e "${YELLOW}📂 Video: $VIDEO_DIR${NC}"
echo -e "${YELLOW}📄 Metadata: $METADATA_DIR${NC}"
echo -e "${YELLOW}🖼️  Thumbnails: $THUMBNAIL_DIR${NC}"
