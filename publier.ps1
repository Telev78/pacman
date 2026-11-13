# Produit les archives à distribuer :
#     dist\PACMAN-<version>-windows-x64.zip
#     dist\PACMAN-<version>-linux-x64.tar.gz
#
# Utilisation, à la racine du dépôt :
#     powershell -ExecutionPolicy Bypass -File .\publier.ps1 -Version 1.0
# Sans -Version, la date du jour est utilisée.
#
# Chaque archive contient un dossier PACMAN avec l'exécutable (fichier unique autonome) et deux notices :
# LISEZMOI.txt (français) et README.txt (anglais).
# L'archive Linux est un .tar.gz écrit ici même, avec le droit d'exécution (0755) sur le programme :
# l'utilisateur n'a pas besoin de faire "chmod +x".

param([string]$Version = '')

$ErrorActionPreference = 'Stop'

if (-not (Test-Path 'PACMAN.slnx')) {
    throw "Lancez ce script à la racine du dépôt (là où se trouve PACMAN.slnx)."
}
if ($Version -eq '') { $Version = (Get-Date -Format 'yyyyMMdd') }

# Petit écrivain de .tar.gz (format USTAR), car Windows PowerShell 5 n'a pas d'outil qui fixe les droits Unix
if (-not ('TarGz' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class TarGz
{
    // names : chemins dans l'archive ("PACMAN/" pour un dossier) ; sources : fichier à lire (null pour un dossier)
    public static void Create(string archivePath, string[] names, string[] sources, int[] modes)
    {
        using (FileStream file = File.Create(archivePath))
        using (GZipStream gzip = new GZipStream(file, CompressionMode.Compress))
        {
            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            for (int i = 0; i < names.Length; i++)
            {
                bool isDirectory = string.IsNullOrEmpty(sources[i]);   // PowerShell convertit $null en chaîne vide
                byte[] data = isDirectory ? new byte[0] : File.ReadAllBytes(sources[i]);

                byte[] header = new byte[512];
                Put(header, 0, 100, names[i]);
                PutOctal(header, 100, 8, modes[i]);
                PutOctal(header, 108, 8, 0);                  // uid
                PutOctal(header, 116, 8, 0);                  // gid
                PutOctal(header, 124, 12, data.Length);
                PutOctal(header, 136, 12, now);
                header[156] = (byte)(isDirectory ? '5' : '0');
                Put(header, 257, 6, "ustar");                 // signature (terminée par un octet nul)
                Put(header, 263, 2, "00");
                Put(header, 265, 32, "root");
                Put(header, 297, 32, "root");

                for (int k = 148; k < 156; k++) header[k] = (byte)' ';   // pendant le calcul, le champ vaut des espaces
                int sum = 0;
                for (int k = 0; k < 512; k++) sum += header[k];
                string checksum = Convert.ToString(sum, 8).PadLeft(6, '0');
                for (int k = 0; k < 6; k++) header[148 + k] = (byte)checksum[k];
                header[154] = 0;
                header[155] = (byte)' ';

                gzip.Write(header, 0, 512);
                if (data.Length > 0)
                {
                    gzip.Write(data, 0, data.Length);
                    int padding = (512 - (data.Length % 512)) % 512;
                    if (padding > 0) gzip.Write(new byte[padding], 0, padding);
                }
            }

            gzip.Write(new byte[1024], 0, 1024);              // fin d'archive : deux blocs vides
        }
    }

    private static void Put(byte[] block, int offset, int length, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length > length) throw new ArgumentException("Nom trop long pour l'archive : " + text);
        Array.Copy(bytes, 0, block, offset, bytes.Length);
    }

    private static void PutOctal(byte[] block, int offset, int length, long value)
    {
        string text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
        Put(block, offset, length - 1, text);
    }
}
'@
}

$cibles = @(
    @{ Profil = 'Windows-x64'; Rid = 'win-x64';   Nom = 'windows-x64'; Exe = 'PACMAN.exe'; Notice = 'LISEZMOI-windows.txt'; NoticeEn = 'README-windows.txt'; Format = 'zip' },
    @{ Profil = 'Linux-x64';   Rid = 'linux-x64'; Nom = 'linux-x64';   Exe = 'PACMAN';     Notice = 'LISEZMOI-linux.txt';   NoticeEn = 'README-linux.txt';   Format = 'tar.gz' }
)

if (Test-Path 'dist') { Remove-Item 'dist' -Recurse -Force }
New-Item -ItemType Directory -Path 'dist' | Out-Null
$racineDepot = (Get-Location).Path

foreach ($c in $cibles) {
    Write-Host ""
    Write-Host "=== Publication $($c.Profil) ===" -ForegroundColor Cyan

    # Sortie propre : on supprime l'ancienne publication de ce profil
    $sortie = "PACMAN.Sdl\bin\Release\net10.0\publish\$($c.Rid)"
    if (Test-Path $sortie) { Remove-Item $sortie -Recurse -Force }

    dotnet publish PACMAN.Sdl -c Release "-p:PublishProfile=$($c.Profil)"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish a échoué pour $($c.Profil)." }

    $exe = Join-Path $sortie $c.Exe
    if (-not (Test-Path $exe)) { throw "Exécutable introuvable : $exe" }
    $notice = "packaging\$($c.Notice)"
    $noticeEn = "packaging\$($c.NoticeEn)"

    $archive = Join-Path $racineDepot "dist\PACMAN-$Version-$($c.Nom).$($c.Format)"

    if ($c.Format -eq 'zip') {
        $dossier = Join-Path $racineDepot "dist\PACMAN-$Version-$($c.Nom)\PACMAN"
        New-Item -ItemType Directory -Path $dossier -Force | Out-Null
        Copy-Item $exe $dossier
        Copy-Item $notice (Join-Path $dossier 'LISEZMOI.txt')
        Copy-Item $noticeEn (Join-Path $dossier 'README.txt')
        Compress-Archive -Path $dossier -DestinationPath $archive
        Remove-Item (Split-Path $dossier -Parent) -Recurse -Force
    }
    else {
        # 493 = 0755 (rwxr-xr-x) pour le dossier et le programme ; 420 = 0644 (rw-r--r--) pour les notices
        [TarGz]::Create($archive,
            [string[]]@('PACMAN/', 'PACMAN/PACMAN', 'PACMAN/LISEZMOI.txt', 'PACMAN/README.txt'),
            [string[]]@($null, (Resolve-Path $exe).Path, (Resolve-Path $notice).Path, (Resolve-Path $noticeEn).Path),
            [int[]]@(493, 493, 420, 420))
    }

    $taille = [math]::Round((Get-Item $archive).Length / 1MB, 1)
    Write-Host "-> $archive ($taille Mo)" -ForegroundColor Green
}

Write-Host ""
Write-Host "Terminé. Archives dans le dossier dist\."
