
from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse, HTMLResponse
import librosa
import numpy as np
import tempfile
from scipy.stats import pearsonr

app = FastAPI()

# app.mount("/static", StaticFiles(directory="static"), name="static")

@app.get("/", response_class=HTMLResponse)
def main_form():
    return """
    <html>
        <head><title>Upload MP3 File</title></head>
        <body>
            <h2>Upload MP3 File to Analyze</h2>
            <form action="/analyze-audio/" enctype="multipart/form-data" method="post">
                <input name="file" type="file" accept=".mp3" />
                <input type="submit" value="Upload and Analyze" />
            </form>
        </body>
    </html>
    """

def get_key_and_mode(chroma_vector):
    krumhansl_major = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09,
                                2.52, 5.19, 2.39, 3.66, 2.29, 2.88])
    krumhansl_minor = np.array([6.33, 2.68, 3.52, 5.38, 2.60, 3.53,
                                2.54, 4.75, 3.98, 2.69, 3.34, 3.17])
    note_names = ['C', 'C#', 'D', 'D#', 'E', 'F',
                  'F#', 'G', 'G#', 'A', 'A#', 'B']

    chroma_vector = chroma_vector / np.linalg.norm(chroma_vector)
    best_corr = -1
    best_key = None
    best_mode = None

    for i in range(12):
        corr_major, _ = pearsonr(chroma_vector, np.roll(krumhansl_major, i))
        corr_minor, _ = pearsonr(chroma_vector, np.roll(krumhansl_minor, i))

        if corr_major > best_corr:
            best_corr = corr_major
            best_key = note_names[i]
            best_mode = "major"

        if corr_minor > best_corr:
            best_corr = corr_minor
            best_key = note_names[i]
            best_mode = "minor"

    return best_key, best_mode

def extract_features(file_path):
    y, sr = librosa.load(file_path, sr=None, mono=True)

    tempo, _ = librosa.beat.beat_track(y=y, sr=sr)
    energy = np.mean(librosa.feature.rms(y=y))
    centroid = np.mean(librosa.feature.spectral_centroid(y=y, sr=sr))
    zcr = np.mean(librosa.feature.zero_crossing_rate(y))
    duration = librosa.get_duration(y=y, sr=sr)
    chroma = librosa.feature.chroma_stft(y=y, sr=sr)
    chroma_mean = np.mean(chroma, axis=1)
    mfcc = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
    mfcc_mean = [np.mean(coef) for coef in mfcc]
    danceability = np.mean(librosa.feature.tempogram(y=y, sr=sr))
    acousticness = np.mean(librosa.feature.spectral_flatness(y=y))

    bpm = tempo if tempo else 0
    key, mode = get_key_and_mode(chroma_mean)
    note_names = ['C', 'C#', 'D', 'D#', 'E', 'F',
                  'F#', 'G', 'G#', 'A', 'A#', 'B']

    return {
        "tempo": float(bpm),
        "key": key,
        "key_number": note_names.index(key),
        "mode": mode,
        "mode_number": 1 if mode == "major" else 0,
        "energy": float(energy),
        "danceability": float(danceability),
        "acousticness": float(acousticness),
        "spectral_centroid": float(centroid),
        "zero_crossing_rate": float(zcr),
        "chroma_mean": chroma_mean.tolist(),
        "mfcc_mean": [float(x) for x in mfcc_mean],
        "duration": float(duration)
    }

@app.post("/analyze-audio/")
async def analyze_audio(file: UploadFile = File(...)):
    with tempfile.NamedTemporaryFile(delete=False, suffix=".mp3") as tmp:
        tmp.write(await file.read())
        tmp_path = tmp.name

    try:
        features = extract_features(tmp_path)
    except Exception as e:
        return JSONResponse(status_code=500, content={"error": str(e)})

    return JSONResponse(content=features)
