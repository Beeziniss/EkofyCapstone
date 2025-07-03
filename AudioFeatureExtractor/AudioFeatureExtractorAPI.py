import grpc
from concurrent import futures
import audio_analysis_pb2 as pb2
import audio_analysis_pb2_grpc as pb2_grpc
import librosa
import numpy as np
from scipy.stats import pearsonr
import io

note_names = ['C', 'C#', 'D', 'D#', 'E', 'F',
              'F#', 'G', 'G#', 'A', 'A#', 'B']

def get_key_and_mode(chroma_vector):
    krumhansl_major = np.array([6.35, 2.23, 3.48, 2.33, 4.38, 4.09,
                                2.52, 5.19, 2.39, 3.66, 2.29, 2.88])
    krumhansl_minor = np.array([6.33, 2.68, 3.52, 5.38, 2.60, 3.53,
                                2.54, 4.75, 3.98, 2.69, 3.34, 3.17])

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

class AudioAnalyzerService(pb2_grpc.AudioAnalyzerServicer):
    def AnalyzeWav(self, request, context):
        try:
            y, sr = librosa.load(io.BytesIO(request.wav_data), sr=None, mono=True)
            tempo, _ = librosa.beat.beat_track(y=y, sr=sr)
            energy = np.mean(librosa.feature.rms(y=y))
            centroid = np.mean(librosa.feature.spectral_centroid(y=y, sr=sr))
            zcr = np.mean(librosa.feature.zero_crossing_rate(y))
            duration = librosa.get_duration(y=y, sr=sr)
            chroma = librosa.feature.chroma_stft(y=y, sr=sr)
            chroma_mean = np.mean(chroma, axis=1)
            mfcc = librosa.feature.mfcc(y=y, sr=sr, n_mfcc=13)
            mfcc_mean = [float(np.mean(coef)) for coef in mfcc]
            danceability = np.mean(librosa.feature.tempogram(y=y, sr=sr))
            acousticness = np.mean(librosa.feature.spectral_flatness(y=y))

            key, mode = get_key_and_mode(chroma_mean)
            bpm = tempo if isinstance(tempo, float) else float(tempo[0])

            return pb2.AudioFeaturesReply(
                tempo=float(bpm),
                key=key,
                key_number=note_names.index(key),
                mode=mode,
                mode_number=1 if mode == "major" else 0,
                energy=float(energy),
                danceability=float(danceability),
                acousticness=float(acousticness),
                spectral_centroid=float(centroid),
                zero_crossing_rate=float(zcr),
                duration=float(duration),
                chroma_mean=chroma_mean.tolist(),
                mfcc_mean=mfcc_mean
            )
        except Exception as e:
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return pb2.AudioFeaturesReply()

def serve():
    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=10),

        # Cho phép nhận file đến 50MB
        options=[
            ("grpc.max_receive_message_length", 50 * 1024 * 1024),  
            ("grpc.max_send_message_length", 50 * 1024 * 1024)
        ]
    )

    pb2_grpc.add_AudioAnalyzerServicer_to_server(AudioAnalyzerService(), server)
    server.add_insecure_port('[::]:50051')
    server.start()
    print("gRPC server started on port 50051")
    server.wait_for_termination()

if __name__ == "__main__":
    serve()