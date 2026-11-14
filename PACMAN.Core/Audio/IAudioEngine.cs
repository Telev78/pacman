using System;

namespace PacmanWinForms
{
    /// <summary>
    /// Ce que le jeu demande à un moteur audio. L'implémentation dépend de la plateforme
    /// (aujourd'hui NAudio sous Windows ; plus tard SDL sous Windows, Linux et macOS).
    /// </summary>
    public interface IAudioEngine : IDisposable
    {
        /// <summary>Volume général, de 0 (muet) à 1 (maximum).</summary>
        float Volume { get; set; }

        /// <summary>Charge les sons d'un jeu (dossier de ressources, ex. "asset.pacman.sounds.") en remplacement des précédents.</summary>
        void LoadSoundPack(string resourceFolder);

        /// <summary>Joue un son depuis le début (relance le son s'il jouait déjà). Peut boucler.</summary>
        void Play(SoundId id, bool loop = false);

        /// <summary>Joue un premier son puis enchaîne sans trou sur un second, éventuellement en boucle.</summary>
        void PlaySequence(SoundId first, SoundId next, bool loopNext = true);

        /// <summary>Coupe un son, qu'il soit en cours ou l'enchaînement d'une séquence.</summary>
        void Stop(SoundId id);

        /// <summary>Coupe tous les sons.</summary>
        void StopAll();
    }
}
