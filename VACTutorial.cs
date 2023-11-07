using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

public class VACTutorial : MonoBehaviour
{
    protected Callback<ValidateAuthTicketResponse_t> m_ValidateAuthTicketResponse; 
    private string Error = string.Empty;
    private bool cheater = false;

    private void Awake()
    {
        m_ValidateAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.Create(OnValidateAuthTicketResponse);
    }

    private void Start()
    {
        // full steam support with session auth and VAC ban
        if (SteamManager.Initialized)
        {
            // Steam Support
            if (SteamAPI.RestartAppIfNecessary(SteamUtils.GetAppID()))
            {
                SteamAPI.Shutdown();
                Application.Quit();
            }

            // steam user auth
            byte[] pendingTicket = new byte[1024];
            int ptMax = 256;
            uint ptLen = 256;

            Steamworks.SteamNetworkingIdentity steamNetworkingIdentity = new SteamNetworkingIdentity();
            steamNetworkingIdentity.SetSteamID(SteamUser.GetSteamID());

            HAuthTicket ticket = SteamUser.GetAuthSessionTicket(pendingTicket, ptMax, out ptLen, ref steamNetworkingIdentity);
            EBeginAuthSessionResult ebas = SteamUser.BeginAuthSession(pendingTicket, (int)ptLen, SteamUser.GetSteamID());

            if (ebas != EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
            {
                // end session
                SteamUser.EndAuthSession(SteamUser.GetSteamID());
                //  and retry
                ticket = SteamUser.GetAuthSessionTicket(pendingTicket, ptMax, out ptLen, ref steamNetworkingIdentity);
                ebas = SteamUser.BeginAuthSession(pendingTicket, (int)ptLen, SteamUser.GetSteamID());
                // end if it still does not work
                if (ebas != EBeginAuthSessionResult.k_EBeginAuthSessionResultOK)
                {
                    SteamUser.EndAuthSession(SteamUser.GetSteamID());
                    SteamAPI.Shutdown();
                    Application.Quit();
                }
            }

        }
    }

    void OnValidateAuthTicketResponse(ValidateAuthTicketResponse_t pCallback)
    {

        Debug.Log("[" + ValidateAuthTicketResponse_t.k_iCallback + " - ValidateAuthTicketResponse] - " + pCallback.m_SteamID + " -- " + pCallback.m_eAuthSessionResponse + " -- " + pCallback.m_OwnerSteamID);

        bool flagged = false;

        // filters
        if (pCallback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseVACBanned)
        {
            // banned by VAC, tell the user they are banned by VAC
            flagged = true;
            cheater = true;
            Error = "You are banned from this game by VAC";
            //ErrorText.GetComponent<TMP_InputField>().text = "You are banned from this game by VAC";
        }
        if (pCallback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponsePublisherIssuedBan)
        {
            // banned by VAC, tell the user they are banned by VAC
            Error = "You are banned from this game by Ground And Pound Gaming";
            //ErrorText.GetComponent<TMP_InputField>().text = "You are banned from this game by Ground And Pound Gaming";
            cheater = true;
            flagged = true;
        }
        if (pCallback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut)
        {
            // should ban this user when VAC can not check and times out, tell user they will be banned if they do not allow VAC
            flagged = true;
            cheater = true;
            Error = "VAC is taking too long, please reload";
            //ErrorText.GetComponent<TMP_InputField>().text = "VAC is taking too long, please reload";
        }
        if (pCallback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseLoggedInElseWhere)
        {
            // tell user they are logged in elsewhere, then make them quit
            flagged = true;
            Error = "You are logged in elsewhere, please reload";
            //ErrorText.GetComponent<TMP_InputField>().text = "You are logged in elsewhere, please reload";
        }

        // something did not check out lets make them exit out until it does go well
        if (pCallback.m_eAuthSessionResponse != EAuthSessionResponse.k_EAuthSessionResponseOK)
        {
            // banned by VAC, tell the user they are banned by VAC
            //ErrorText.GetComponent<TMP_InputField>().text = "Something is wrong with connecting to VAC";
            Error = "Something is wrong with connecting to VAC, Please Reload";
            flagged = true;
        }

        if (flagged)
        {
            Debug.Log("Flagged - " + Error);
            SteamUser.EndAuthSession(SteamUser.GetSteamID());
            SteamAPI.Shutdown();
            Application.Quit();
        }
    }

    private void OnDestroy()
    {
        // to prevent object destroys from a cheat client 
        SteamUser.EndAuthSession(SteamUser.GetSteamID());
        SteamAPI.Shutdown();
        Application.Quit();
    }

}