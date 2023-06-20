using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UpdateIpAddress
{
    private static string ipaddress;
    /// <summary>
    /// Lädt die aktuelle IP-Adresse des Servers und vergleicht dies mit der Game-DNS-Adresse. 
    /// Falls die Game-DNS-Adresse != der des Servers ist, wird diese bei No-IP.com aktualisiert. Wenn die dazu notwendigen Daten eingegeben wurden. 
    /// </summary>
    /// <returns>Ergebnis der Aktualisierung der DNS-Adresse</returns>
    public static IEnumerator UpdateNoIP_DNS()
    {
        // Lade aktuelle IP-Adresse
        //string ipaddress = "";
        // Lade DNS-IP-Adresse
        //IPAddress[] domainip;
        try
        {
            //ipaddress = new WebClient().DownloadString("https://api.ipify.org");
            WebClient wc = new WebClient();
            wc.DownloadStringCompleted += new DownloadStringCompletedEventHandler(UpdatelocalIp);
            wc.DownloadStringAsync(new Uri("https://api.ipify.org"));
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Could not resolve host " + Config.SERVER_CONNECTION_IP, e);
            Config.HAUPTMENUE_FEHLERMELDUNG += "\nBitte überprüfe deine Internetverbindung!";
            Config.LOBBY_FEHLERMELDUNG = "Bitte überprüfe deine Internetverbindung!";
            yield break;
        }
        yield break;
        /*
        try
        {
            domainip = Dns.GetHostAddresses(Config.SERVER_CONNECTION_IP);
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Could not resolve host domain " + Config.SERVER_CONNECTION_IP, e);
            Config.HAUPTMENUE_FEHLERMELDUNG += "\nBitte überprüfe deine Internetverbindung!";
            Config.LOBBY_FEHLERMELDUNG = "Bitte überprüfe deine Internetverbindung!";
            yield break;
        }
        yield return null;

        Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "Aktuelle IP: " + ipaddress + "  DNS-IP: " + domainip[0].ToString());
        // Wenn die DNS-IP gleich der aktuellen des Servers ist, dann muss kein IP Update durchgeführt werden
        if (ipaddress == domainip[0].ToString())
        {
            Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "DNS - IP ist aktuell.");
            yield break;
        }

        // Noch keine Kontodaten vorhanden
        if (!File.Exists(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            string noiptxt = Application.persistentDataPath + @"/No-IP Settings.txt";
            if (!File.Exists(noiptxt))
            {
                using (FileStream fs = File.Create(noiptxt))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes("No-IP_Benutzername: " +
                        "\nNo-IP_Passwort: " +
                        "\nNo-IP_Hostname: ");
                    fs.Write(info, 0, info.Length);
                }
            }
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Autostart wurde abgebrochen, keine Kontodaten zum Aktualisieren der DNS-IP vorhanden.");
            yield break;
        }

        // Lade daten
        string username = "";
        string password = "";
        string hostname = "";
        foreach (string zeile in LadeDateien.listInhalt(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            if (zeile.StartsWith("No-IP_Benutzername: "))
            {
                username = zeile.Substring("No-IP_Benutzername: ".Length);
            }
            else if (zeile.StartsWith("No-IP_Passwort: "))
            {
                password = zeile.Substring("No-IP_Passwort: ".Length);
            }
            else if (zeile.StartsWith("No-IP_Hostname: "))
            {
                hostname = zeile.Substring("No-IP_Hostname: ".Length);
            }
            else
            {
                Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Dateiinhalt ist fehlerhaft: Datei: No - IP Settings.txt-- Zeile: "+zeile);
                yield break;
            }
        }
        if (username.Length == 0 || password.Length == 0 || hostname.Length == 0)
            yield break;
        yield return null;

        // No-IP-Update-URL erstellen
        string url = $"https://dynupdate.no-ip.com/nic/update?hostname={hostname}&myip={ipaddress}";

        // HTTP-Anfrage erstellen
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));

        // HTTP-Anfrage senden
        HttpWebResponse response;
        try
        {
            response = (HttpWebResponse)request.GetResponse();
        }
        catch (Exception e)
        {
            Config.LOBBY_FEHLERMELDUNG = "IP konnte nicht aktualisiert werden. Überprüfe deine NoIP Eingabe.";
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Konnte keine Webanfrage senden! IP wurde nicht aktualisiert!", e);
            yield break;
        }
        yield return null;

        // Serverantwort lesen
        string result = new StreamReader(response.GetResponseStream()).ReadToEnd();

        // Ergebnis auf Erfolg oder Fehler prüfen
        if (result.Contains("good") || result.Contains("nochg"))
        {
            Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "IP-Adresse erfolgreich aktualisiert.");
            yield break;
        }
        else
        {
            Config.LOBBY_FEHLERMELDUNG = "NoIP hat die Updateanfrage abgelehnt. IP Adresse wurde nicht aktualisiert.";
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Fehler beim Aktualisieren der IP - Adresse. AktuelleIP: "+ ipaddress +" HTTP - Result: "+ result);
            yield break;
        }*/
    }
    private static void UpdatelocalIp(System.Object sender, DownloadStringCompletedEventArgs eventargs)
    {
        if (!eventargs.Cancelled && eventargs.Error == null)
        {
            string textString = (string)eventargs.Result;
            ipaddress = textString;
        }
        else
        {
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdatelocalIp", "Could not resolve host " + Config.SERVER_CONNECTION_IP, eventargs.Error);
            Config.HAUPTMENUE_FEHLERMELDUNG += "\nBitte überprüfe deine Internetverbindung!";
            Config.LOBBY_FEHLERMELDUNG = "Bitte überprüfe deine Internetverbindung!";
            return;
        }

        IPAddress[] domainip;
        try
        {
            domainip = Dns.GetHostAddresses(Config.SERVER_CONNECTION_IP);
        }
        catch (Exception e)
        {
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Could not resolve host domain " + Config.SERVER_CONNECTION_IP, e);
            Config.HAUPTMENUE_FEHLERMELDUNG += "\nBitte überprüfe deine Internetverbindung!";
            Config.LOBBY_FEHLERMELDUNG = "Bitte überprüfe deine Internetverbindung!";
            return;
        }
        Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "Aktuelle IP: " + ipaddress + "  DNS-IP: " + domainip[0].ToString());
        // Wenn die DNS-IP gleich der aktuellen des Servers ist, dann muss kein IP Update durchgeführt werden
        if (ipaddress == domainip[0].ToString())
        {
            Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "DNS - IP ist aktuell.");
            return;
        }

        // Noch keine Kontodaten vorhanden
        if (!File.Exists(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            string noiptxt = Application.persistentDataPath + @"/No-IP Settings.txt";
            if (!File.Exists(noiptxt))
            {
                using (FileStream fs = File.Create(noiptxt))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes("No-IP_Benutzername: " +
                        "\nNo-IP_Passwort: " +
                        "\nNo-IP_Hostname: ");
                    fs.Write(info, 0, info.Length);
                }
            }
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Autostart wurde abgebrochen, keine Kontodaten zum Aktualisieren der DNS-IP vorhanden.");
            return;
        }

        // Lade daten
        string username = "";
        string password = "";
        string hostname = "";
        foreach (string zeile in LadeDateien.listInhalt(Application.persistentDataPath + @"/No-IP Settings.txt"))
        {
            if (zeile.StartsWith("No-IP_Benutzername: "))
            {
                username = zeile.Substring("No-IP_Benutzername: ".Length);
            }
            else if (zeile.StartsWith("No-IP_Passwort: "))
            {
                password = zeile.Substring("No-IP_Passwort: ".Length);
            }
            else if (zeile.StartsWith("No-IP_Hostname: "))
            {
                hostname = zeile.Substring("No-IP_Hostname: ".Length);
            }
            else
            {
                Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Dateiinhalt ist fehlerhaft: Datei: No - IP Settings.txt-- Zeile: " + zeile);
                return;
            }
        }
        if (username.Length == 0 || password.Length == 0 || hostname.Length == 0)
            return;

        // No-IP-Update-URL erstellen
        string url = $"https://dynupdate.no-ip.com/nic/update?hostname={hostname}&myip={ipaddress}";

        // HTTP-Anfrage erstellen
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));

        // HTTP-Anfrage senden
        HttpWebResponse response;
        try
        {
            response = (HttpWebResponse)request.GetResponse();
        }
        catch (Exception e)
        {
            Config.LOBBY_FEHLERMELDUNG = "IP konnte nicht aktualisiert werden. Überprüfe deine NoIP Eingabe.";
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Konnte keine Webanfrage senden! IP wurde nicht aktualisiert!", e);
            return;
        }

        // Serverantwort lesen
        string result = new StreamReader(response.GetResponseStream()).ReadToEnd();

        // Ergebnis auf Erfolg oder Fehler prüfen
        if (result.Contains("good") || result.Contains("nochg"))
        {
            Logging.log(Logging.LogType.Normal, "UpdateIpAddress", "UpdateNoIP_DNS", "IP-Adresse erfolgreich aktualisiert.");
            return;
        }
        else
        {
            Config.LOBBY_FEHLERMELDUNG = "NoIP hat die Updateanfrage abgelehnt. IP Adresse wurde nicht aktualisiert.";
            Logging.log(Logging.LogType.Warning, "UpdateIpAddress", "UpdateNoIP_DNS", "Fehler beim Aktualisieren der IP - Adresse. AktuelleIP: " + ipaddress + " HTTP - Result: " + result);
            return;
        }
    }

}
