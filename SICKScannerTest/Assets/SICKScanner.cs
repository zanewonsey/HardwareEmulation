using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

class PepperFuchsScanPoint
{
    public float   distance;       // 32 - uint
    public ushort amplitude;       // 16

    public PepperFuchsScanPoint(float dist, ushort amp)
    {
        distance = dist;
        amplitude = amp;
    }
}

class PepperlFuchsBasicPacket
{
    // Header Info
    ushort magic;           // 16
    ushort packet_type;     // 16
    uint   packet_size;     // 32
    ushort header_size;     // 16
    byte[] header_padding;  //(0-3) Varies based on header

    // Payload Data
    PepperFuchsScanPoint[] payload;
    byte[] payload_padding; // (0-3) Varies based on payload

    // Checksum
    uint packet_crc;

    public PepperlFuchsBasicPacket()
    {
    }

    // Constructor
    public PepperlFuchsBasicPacket(ushort type)
    {
        magic = 0xA25C;
        packet_type = type;
        packet_size = 16; // payload and payload padding should be added to thispadding
        header_size = 12;
        header_padding = new byte[2];
        payload = null;
        payload_padding = null;
        packet_crc = 0;
    }

    public void AddPayload(PepperFuchsScanPoint[] pl)
    {
        payload = pl;
        if (payload.Length % 2 == 0) payload_padding = new byte[0]; else payload_padding = new byte[1];
        AddPayloadToPacketSize((uint) payload.Length * 6, (uint) payload_padding.Length);
    }

    private void AddPayloadToPacketSize(uint payloadSize, uint paddingSize)
    {
        packet_size += payloadSize + paddingSize;
    }

    public int Size()
    {
        return 2+2+4+2+2+ (payload.Length * 6) + payload_padding.Length+ 4;
    }

    public void Send(UdpClient udpClient)
    {
        int arrayPtr = 0;
        byte[] payloadInBytes = new byte[Size()];
        payloadInBytes[arrayPtr++] = (byte) (magic >> 8 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (magic & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_type >> 8 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_type & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_size >> 24 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_size >> 16 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_size >>  8 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (packet_size & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (header_size >> 8 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte) (header_size & 0xFF);
        for (int i = 0; i < header_padding.Length; i++)
        {
            payloadInBytes[arrayPtr++] = header_padding[i];
        }
        for (int i = 0; i < payload.Length; i++)
        {
            byte[] dist = System.BitConverter.GetBytes(payload[i].distance);
            byte[] amp  = System.BitConverter.GetBytes(payload[i].amplitude);
            System.Buffer.BlockCopy(dist, 0, payloadInBytes, arrayPtr, 4);
            arrayPtr += 4;
            System.Buffer.BlockCopy( amp, 0, payloadInBytes, arrayPtr, 2);
            arrayPtr += 2;
        }
        for (int i = 0; i < payload_padding.Length; i++)
        {
            payloadInBytes[arrayPtr++] = payload_padding[i];
        }
        payloadInBytes[arrayPtr++] = (byte)(packet_crc >> 24 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte)(packet_crc >> 16 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte)(packet_crc >>  8 & 0xFF);
        payloadInBytes[arrayPtr++] = (byte)(packet_crc & 0xFF);
        udpClient.Send(payloadInBytes, Size());
    }
    public void Send(UdpClient udpClient, PepperFuchsScanPoint[] pl)
    {
        AddPayload(pl);
        Send(udpClient);
    }
}

public class SICKScanner : MonoBehaviour
{

    public float arcAngle = 360.0F;
    public int numLines = 3601;
    public int maxDist = 8;
    public int scansPerSec = 600;
    private PepperFuchsScanPoint[] ranges;
    private float timer = 0.0F;
    RaycastHit hit;
    Vector3 shootVec;
    UdpClient udpClient;

    void Start()
    {
        ranges = new PepperFuchsScanPoint[numLines];
        udpClient = new UdpClient("127.0.0.1", 12322);
    }

    public void Update()
    {
        DoScan1();
    }

    //Centers scan of arcAngle degrees with numLines rays around "forward". Centering accounts for the "missing" last ray by arcAngle/(2*numLines) degrees.
    //Start and end lines are not exactly on 0 and n degrees but offset by arcAngle/(2*numLines) degrees.
    public void DoScan1()
    {
        if (timer > 1.0 / scansPerSec)
        {
            for (int l = 0; l < numLines; l++)
            {
                shootVec = transform.rotation * Quaternion.AngleAxis(-1 * arcAngle / 2 + (l * arcAngle / numLines) + arcAngle / (2 * numLines), Vector3.up) * Vector3.forward;         
                if (Physics.Raycast(transform.position, shootVec, out hit, maxDist))
                {
                    ranges[l] = new PepperFuchsScanPoint(hit.distance, 0);
                    Debug.DrawLine(transform.position, hit.point, Color.red);
                }
                else
                {
                    ranges[l] = new PepperFuchsScanPoint(maxDist, 0);
                    Debug.DrawLine(transform.position, shootVec * maxDist, Color.blue);
                }
                
                
            }
            timer = 0;
            PepperlFuchsBasicPacket packet = new PepperlFuchsBasicPacket(1);
            packet.Send(udpClient, ranges);
        }
        else timer += Time.deltaTime;
    }

    //Centers scan of arcAngle degrees with numLines rays around "forward". Centering accounts for the "missing" last ray by adding the last "extra" ray.
    //Start and end lines are exactly on 0 and n degrees but there are numLines+1 rays.
    /*public void DoScan2()
    {
        nLines = numLines + 1;
        if (timer > 1.0 / scansPerSec)
        {
            ranges = new float[nLines];
            for (l = 0; l < nLines; l++)
            {
                var shootVec : Vector3 = transform.rotation * Quaternion.AngleAxis(-1 * arcAngle / 2 + (l * arcAngle / numLines), Vector3.up) * Vector3.forward;
                var hit : RaycastHit;
                Debug.DrawRay(transform.position, shootVec, Color.blue);
                if (Physics.Raycast(transform.position, shootVec, hit, maxDist))
                {
                    Debug.DrawLine(transform.position, hit.point, Color.blue);
                    ranges[l] = hit.distance;
                }
                else ranges[l] = maxDist;
            }
            timer = 0;
        }
        else timer += Time.deltaTime;
    }*/
}
