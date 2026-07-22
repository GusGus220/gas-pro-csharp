using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi; // <--- MAGIA: Control absoluto del audio de Windows

namespace GasPro.Services
{
    public class WindowsControlService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;

        public void Mute() => keybd_event(VK_VOLUME_MUTE, 0, 0, 0);
        public void PlayPauseMusic() => keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, 0);

        // 🌟 NUEVO: Control de volumen exacto (0 a 100)
        public void SetVolume(int percentage)
        {
            try
            {
                // Candado de seguridad para que no exploten tus parlantes
                percentage = Math.Max(0, Math.Min(100, percentage));

                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                // Windows maneja el volumen de 0.0f a 1.0f
                device.AudioEndpointVolume.MasterVolumeLevelScalar = percentage / 100f;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error de Volumen] {ex.Message}");
            }
        }

        // 🌟 NUEVO: Sube o baja el volumen de golpe basado en el volumen actual
        public void ChangeVolumeBy(int amount)
        {
            try
            {
                var enumerator = new MMDeviceEnumerator();
                var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                float currentVol = device.AudioEndpointVolume.MasterVolumeLevelScalar * 100;
                SetVolume((int)Math.Round(currentVol) + amount);
            }
            catch { }
        }

        public void OpenApplication(string appName)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = appName,
                    UseShellExecute = true // Permite usar protocolos de Windows
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[Error de Sistema] No pude abrir {appName}: {ex.Message}");
            }
        }
    }
}