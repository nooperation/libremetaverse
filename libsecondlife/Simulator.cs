/*
 * Copyright (c) 2007, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using libsecondlife.Packets;

namespace libsecondlife
{
    /// <summary>
    /// Simulator is a wrapper for a network connection to a simulator and the
    /// Region class representing the block of land in the metaverse
    /// </summary>
    public class Simulator
    {
        /// <summary>
        /// Simulator (region) properties
        /// </summary>
        [Flags]
        public enum RegionFlags
        {
            /// <summary>No flags set</summary>
            None = 0,
            /// <summary>Agents can take damage and be killed</summary>
            AllowDamage = 1 << 0,
            /// <summary>Landmarks can be created here</summary>
            AllowLandmark = 1 << 1,
            /// <summary>Home position can be set in this sim</summary>
            AllowSetHome = 1 << 2,
            /// <summary>Home position is reset when an agent teleports away</summary>
            ResetHomeOnTeleport = 1 << 3,
            /// <summary>Sun does not move</summary>
            SunFixed = 1 << 4,
            /// <summary>No object, land, etc. taxes</summary>
            TaxFree = 1 << 5,
            /// <summary>Disable heightmap alterations (agents can still plant
            /// foliage)</summary>
            BlockTerraform = 1 << 6,
            /// <summary>Land cannot be released, sold, or purchased</summary>
            BlockLandResell = 1 << 7,
            /// <summary>All content is wiped nightly</summary>
            Sandbox = 1 << 8,
            /// <summary></summary>
            NullLayer = 1 << 9,
            /// <summary></summary>
            SkipAgentAction = 1 << 10,
            /// <summary></summary>
            SkipUpdateInterestList = 1 << 11,
            /// <summary>No collision detection for non-agent objects</summary>
            SkipCollisions = 1 << 12,
            /// <summary>No scripts are ran</summary>
            SkipScripts = 1 << 13,
            /// <summary>All physics processing is turned off</summary>
            SkipPhysics = 1 << 14,
            /// <summary></summary>
            ExternallyVisible = 1 << 15,
            /// <summary></summary>
            MainlandVisible = 1 << 16,
            /// <summary></summary>
            PublicAllowed = 1 << 17,
            /// <summary></summary>
            BlockDwell = 1 << 18,
            /// <summary>Flight is disabled (not currently enforced by the sim)</summary>
            NoFly = 1 << 19,
            /// <summary>Allow direct (p2p) teleporting</summary>
            AllowDirectTeleport = 1 << 20,
            /// <summary>Estate owner has temporarily disabled scripting</summary>
            EstateSkipScripts = 1 << 21,
            /// <summary></summary>
            RestrictPushObject = 1 << 22,
            /// <summary>Deny agents with no payment info on file</summary>
            DenyAnonymous = 1 << 23,
            /// <summary>Deny agents with payment info on file</summary>
            DenyIdentified = 1 << 24,
            /// <summary>Deny agents who have made a monetary transaction</summary>
            DenyTransacted = 1 << 25,
            /// <summary></summary>
            AllowParcelChanges = 1 << 26,
            /// <summary></summary>
            AbuseEmailToEstateOwner = 1 << 27
        }

        /// <summary>
        /// 
        /// </summary>
        public enum SimAccess : byte
        {
            /// <summary></summary>
            Min = 0,
            /// <summary></summary>
            Trial = 7,
            /// <summary></summary>
            PG = 13,
            /// <summary></summary>
            Mature = 21,
            /// <summary></summary>
            Down = 254,
            /// <summary></summary>
            NonExistent = 255
        }


        /// <summary>A public reference to the client that this Simulator object
        /// is attached to</summary>
        public SecondLife Client;
        /// <summary></summary>
        public LLUUID ID = LLUUID.Zero;
        /// <summary></summary>
        public ulong Handle;
        /// <summary></summary>
        public string Name = String.Empty;
        /// <summary></summary>
        public byte[] ParcelOverlay = new byte[4096];
        /// <summary></summary>
        public int ParcelOverlaysReceived;
        /// <summary></summary>
        public float TerrainHeightRange00;
        /// <summary></summary>
        public float TerrainHeightRange01;
        /// <summary></summary>
        public float TerrainHeightRange10;
        /// <summary></summary>
        public float TerrainHeightRange11;
        /// <summary></summary>
        public float TerrainStartHeight00;
        /// <summary></summary>
        public float TerrainStartHeight01;
        /// <summary></summary>
        public float TerrainStartHeight10;
        /// <summary></summary>
        public float TerrainStartHeight11;
        /// <summary></summary>
        public float WaterHeight;
        /// <summary></summary>
        public LLUUID SimOwner = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainBase0 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainBase1 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainBase2 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainBase3 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainDetail0 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainDetail1 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainDetail2 = LLUUID.Zero;
        /// <summary></summary>
        public LLUUID TerrainDetail3 = LLUUID.Zero;
        /// <summary></summary>
        public bool IsEstateManager;
        /// <summary></summary>
        public EstateTools Estate;
        /// <summary>Current time dilation of this simulator</summary>
        public float Dilation;
        /// <summary></summary>
        public RegionFlags Flags;
        /// <summary></summary>
        public SimAccess Access;
        /// <summary></summary>
        public float BillableFactor;

        /// <summary>The IP address and port of the server</summary>
        public IPEndPoint IPEndPoint { get { return ipEndPoint; } }
        /// <summary>Whether there is a working connection to the simulator or 
        /// not</summary>
        public bool Connected { get { return connected; } }
        /// <summary>Coarse locations of avatars in this simulator</summary>
        public List<LLVector3> AvatarPositions { get { return avatarPositions; } }

        /// <summary>Used internally to track sim disconnections</summary>
        internal bool DisconnectCandidate = false;
        /// <summary></summary>
        internal ManualResetEvent ConnectedEvent = new ManualResetEvent(false);
        /// <summary></summary>
        internal bool connected;
        /// <summary>Coarse locations of avatars in this simulator</summary>
        internal List<LLVector3> avatarPositions = new List<LLVector3>();
        public ulong SentPackets = 0, RecvPackets = 0;
        public ulong SentBytes = 0, RecvBytes = 0;
        public int ConnectTime = 0, ResentPackets = 0;
        public int SentPings = 0, ReceivedPongs = 0;
        // Calculated bandwidth -- it would be nice to have these calculated
        // on the fly, but this is far, far easier :/  These are bytes per second.
        public int IncomingBPS = 0, OutgoingBPS = 0;

        private NetworkManager Network;
        private uint Sequence = 0;
        private object SequenceLock = new object();
        private byte[] RecvBuffer = new byte[4096];
        private byte[] ZeroBuffer = new byte[8192];
        private byte[] ZeroOutBuffer = new byte[4096];
        private Socket Connection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private AsyncCallback ReceivedData;
        // Packets we sent out that need ACKs from the simulator
        private Dictionary<uint, Packet> NeedAck = new Dictionary<uint, Packet>();
        // Sequence numbers of packets we've received from the simulator
        private Queue<uint> Inbox;//, PingTimes;
        private Queue<ulong> InBytes, OutBytes;

        // ACKs that are queued up to be sent to the simulator
        private SortedList<uint, uint> PendingAcks = new SortedList<uint, uint>();
        private IPEndPoint ipEndPoint;
        private EndPoint endPoint;
        private System.Timers.Timer AckTimer;
        private System.Timers.Timer PingTimer;
        private System.Timers.Timer StatsTimer;

        /* Ping processing, to measure link health */
        public int LastPingSent = 0;
        public byte LastPingID = 0;
        public int LastLag = 0;
        public int MissedPings = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="client">Reference to the SecondLife client</param>
        /// <param name="address">IP address and port of the simulator</param>
        public Simulator(SecondLife client, IPEndPoint address)
        {
            Client = client;

            Estate = new EstateTools(Client);
            Network = client.Network;
            Inbox = new Queue<uint>(Client.Settings.INBOX_SIZE);
            InBytes = new Queue<ulong>(Client.Settings.STATS_QUEUE_SIZE);
            OutBytes = new Queue<ulong>(Client.Settings.STATS_QUEUE_SIZE);
            //PingTimes = new Queue<uint>(Client.Settings.STATS_QUEUE_SIZE); //TODO: Is this here for a purpose?

            // Create an endpoint that we will be communicating with (need it in two 
            // types due to .NET weirdness)
            ipEndPoint = address;
            endPoint = (EndPoint)ipEndPoint;

            // Initialize the callback for receiving a new packet
            ReceivedData = new AsyncCallback(OnReceivedData);

            AckTimer = new System.Timers.Timer(Client.Settings.NETWORK_TICK_LENGTH);
            AckTimer.Elapsed += new System.Timers.ElapsedEventHandler(AckTimer_Elapsed);

            StatsTimer = new System.Timers.Timer(1000);
            StatsTimer.Elapsed += new System.Timers.ElapsedEventHandler(StatsTimer_Elapsed);

            PingTimer = new System.Timers.Timer(Client.Settings.PING_INTERVAL);
            PingTimer.Elapsed += new System.Timers.ElapsedEventHandler(PingTimer_Elapsed);
        }

        /// <summary>
        /// Attempt to connect to this simulator
        /// </summary>
        /// <param name="moveToSim">Whether to move our agent in to this sim or not</param>
        /// <returns>True if the connection succeeded or connection status is
        /// unknown, false if there was a failure</returns>
        public bool Connect(bool moveToSim)
        {
            if (connected)
            {
                Client.Self.CompleteAgentMovement(this);
                return true;
            }

            // Start the timers
            AckTimer.Start();
            StatsTimer.Start();
            PingTimer.Enabled = Client.Settings.SEND_PINGS;

            Client.Log("Connecting to " + this.ToString(), Helpers.LogLevel.Info);

            try
            {
                ConnectedEvent.Reset();

                // Associate this simulator's socket with the given ip/port and start listening
                Connection.Connect(endPoint);
                Connection.BeginReceiveFrom(RecvBuffer, 0, RecvBuffer.Length, SocketFlags.None, ref endPoint, ReceivedData, null);

                // Send the UseCircuitCode packet to initiate the connection
                UseCircuitCodePacket use = new UseCircuitCodePacket();
                use.CircuitCode.Code = Network.CircuitCode;
                use.CircuitCode.ID = Network.AgentID;
                use.CircuitCode.SessionID = Network.SessionID;

                // Send the initial packet out
                SendPacket(use, true);

                ConnectTime = Environment.TickCount;

                // Move our agent in to the sim to complete the connection
                if (moveToSim) Client.Self.CompleteAgentMovement(this);

                if (Client.Settings.SEND_AGENT_UPDATES)
                    Client.Self.Status.SendUpdate(true, this);

                if (!ConnectedEvent.WaitOne(Client.Settings.SIMULATOR_TIMEOUT, false))
                {
                    Client.Log("Giving up on waiting for RegionHandshake for " + this.ToString(), 
                        Helpers.LogLevel.Warning);
                    connected = true;
                }

                return true;
            }
            catch (Exception e)
            {
                Client.Log(e.ToString(), Helpers.LogLevel.Error);
            }

            return false;
        }

        /// <summary>
        /// Disconnect from this simulator
        /// </summary>
        public void Disconnect()
        {
            connected = false;
            AckTimer.Stop();
            StatsTimer.Stop();
            if (Client.Settings.SEND_PINGS) PingTimer.Stop();

            // Make sure the socket is hooked up
            if (!Connection.Connected) return;

            // Try to send the CloseCircuit notice
            CloseCircuitPacket close = new CloseCircuitPacket();

            // There's a high probability of this failing if the network is
            // disconnecting, so don't even bother logging the error
            try { Connection.Send(close.ToBytes()); }
            catch (SocketException) { }

            // Shut the socket communication down
            try { Connection.Shutdown(SocketShutdown.Both); }
            catch (SocketException) { }
        }

        /// <summary>
        /// Sends a packet
        /// </summary>
        /// <param name="packet">Packet to be sent</param>
        /// <param name="incrementSequence">Increment sequence number?</param>
        public void SendPacket(Packet packet, bool incrementSequence)
        {
            byte[] buffer;
            int bytes;

            // Keep track of when this packet was sent out
            packet.TickCount = Environment.TickCount;

            if (incrementSequence)
            {
                // Set the sequence number
                lock (SequenceLock)
                {
                    if (Sequence > Client.Settings.MAX_SEQUENCE)
                        Sequence = 1;
                    else
                        Sequence++;
                    packet.Header.Sequence = Sequence;
                }

                // Scrub any appended ACKs since all of the ACK handling is done here
                if (packet.Header.AckList.Length > 0)
                    packet.Header.AckList = new uint[0];

                packet.Header.AppendedAcks = false;

                if (packet.Header.Reliable)
                {
                    lock (NeedAck)
                    {
                        if (!NeedAck.ContainsKey(packet.Header.Sequence))
                            NeedAck.Add(packet.Header.Sequence, packet);
                        else
                            Client.Log("Attempted to add a duplicate sequence number (" +
                                packet.Header.Sequence + ") to the NeedAck dictionary for packet type " +
                                packet.Type.ToString(), Helpers.LogLevel.Warning);
                    }

                    // Don't append ACKs to resent packets, in case that's what was causing the
                    // delivery to fail
                    if (!packet.Header.Resent)
                    {
                        // Append any ACKs that need to be sent out to this packet
                        lock (PendingAcks)
                        {
                            if (PendingAcks.Count > 0 && PendingAcks.Count < Client.Settings.MAX_APPENDED_ACKS &&
                                packet.Type != PacketType.PacketAck &&
                                packet.Type != PacketType.LogoutRequest)
                            {
                                packet.Header.AckList = new uint[PendingAcks.Count];

                                for (int i = 0; i < PendingAcks.Count; i++)
                                    packet.Header.AckList[i] = PendingAcks.Values[i];

                                PendingAcks.Clear();
                                packet.Header.AppendedAcks = true;
                            }
                        }
                    }
                }
            }

            // Serialize the packet
            buffer = packet.ToBytes();
            bytes = buffer.Length;
            SentBytes += (ulong)bytes;
            SentPackets++;

            try
            {
                // Zerocode if needed
                if (packet.Header.Zerocoded)
                {
                    lock (ZeroOutBuffer)
                    {
                        bytes = Helpers.ZeroEncode(buffer, bytes, ZeroOutBuffer);
                        Connection.Send(ZeroOutBuffer, bytes, SocketFlags.None);
                    }
                }
                else
                {
                    Connection.Send(buffer, bytes, SocketFlags.None);
                }
            }
            catch (SocketException)
            {
                Client.Log("Tried to send a " + packet.Type.ToString() + " on a closed socket, shutting down " +
                    this.ToString(), Helpers.LogLevel.Info);

                Network.DisconnectSim(this);
                return;
            }
        }

        /// <summary>
        /// Send a raw byte array payload as a packet
        /// </summary>
        /// <param name="payload">The packet payload</param>
        /// <param name="setSequence">Whether the second, third, and fourth bytes
        /// should be modified to the current stream sequence number</param>
        public void SendPacket(byte[] payload, bool setSequence)
        {
            if (Client.Settings.OUTBOUND_THROTTLE) DoThrottle();

            try
            {
                if (setSequence && payload.Length > 3)
                {
                    lock (SequenceLock)
                    {
                        payload[1] = (byte)(Sequence >> 16);
                        payload[2] = (byte)(Sequence >> 8);
                        payload[3] = (byte)(Sequence % 256);
                        Sequence++;
                    }
                }

                SentBytes += (ulong)payload.Length;
                SentPackets++;
                Connection.Send(payload, payload.Length, SocketFlags.None);
            }
            catch (SocketException)
            {
                Client.Log("Tried to send a " + payload.Length + " byte payload on a closed socket, shutting down " +
                    this.ToString(), Helpers.LogLevel.Info);

                Network.DisconnectSim(this);
                return;
            }
        }

        public void SendPing()
        {
            StartPingCheckPacket ping = new StartPingCheckPacket();
            ping.PingID.PingID = LastPingID++;
            ping.PingID.OldestUnacked = 0; // FIXME
            ping.Header.Reliable = false;
            SendPacket(ping, true);
            LastPingSent = Environment.TickCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="west"></param>
        /// <param name="south"></param>
        /// <param name="east"></param>
        /// <param name="north"></param>
        public void ParcelSubdivide(float west, float south, float east, float north)
        {
            ParcelDividePacket divide = new ParcelDividePacket();
            divide.AgentData.AgentID = Client.Network.AgentID;
            divide.AgentData.SessionID = Client.Network.SessionID;
            divide.ParcelData.East = east;
            divide.ParcelData.North = north;
            divide.ParcelData.South = south;
            divide.ParcelData.West = west;

            SendPacket(divide, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="west"></param>
        /// <param name="south"></param>
        /// <param name="east"></param>
        /// <param name="north"></param>
        public void ParcelJoin(float west, float south, float east, float north)
        {
            ParcelJoinPacket join = new ParcelJoinPacket();
            join.AgentData.AgentID = Client.Network.AgentID;
            join.AgentData.SessionID = Client.Network.SessionID;
            join.ParcelData.East = east;
            join.ParcelData.North = north;
            join.ParcelData.South = south;
            join.ParcelData.West = west;

            SendPacket(join, true);
        }

        /// <summary>
        /// Returns Simulator Name as a String
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Name.Length > 0)
                return Name + " (" + ipEndPoint.ToString() + ")";
            else
                return "(" + ipEndPoint.ToString() + ")";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Simulator sim = obj as Simulator;
            if (sim == null)
                return false;
            return (ipEndPoint.Equals(sim.ipEndPoint));
        }

        public static bool operator ==(Simulator lhs, Simulator rhs)
        {
            // If both are null, or both are same instance, return true
            if (System.Object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
            {
                return false;
            }

            return lhs.ipEndPoint.Equals(rhs.ipEndPoint);
        }

        public static bool operator !=(Simulator lhs, Simulator rhs)
        {
            return !(lhs == rhs);
        }

        private void DoThrottle()
        {
            int throttle;

            if (OutgoingBPS > Client.Settings.OUTBOUND_THROTTLE_RATE)
            {
                throttle = (int)((OutgoingBPS - Client.Settings.OUTBOUND_THROTTLE_RATE) * 2000 /
                    Client.Settings.OUTBOUND_THROTTLE_RATE);

                Client.DebugLog(String.Format("Simulator {0} throttling for {1}ms", this.ToString(), throttle));
                // FIXME: When the outgoing message pumps are in place we won't need to throttle by locking up
                // the application
                Thread.Sleep(throttle);
            }
        }

        /// <summary>
        /// Sends out pending acknowledgements
        /// </summary>
        private void SendAcks()
        {
            lock (PendingAcks)
            {
                if (PendingAcks.Count > 0)
                {
                    if (PendingAcks.Count > 250)
                    {
                        Client.Log("Too many ACKs queued up!", Helpers.LogLevel.Error);
                        return;
                    }

                    PacketAckPacket acks = new PacketAckPacket();
                    acks.Header.Reliable = false;
                    acks.Packets = new PacketAckPacket.PacketsBlock[PendingAcks.Count];

                    for (int i = 0; i < PendingAcks.Count; i++)
                    {
                        acks.Packets[i] = new PacketAckPacket.PacketsBlock();
                        acks.Packets[i].ID = PendingAcks.Values[i];
                    }

                    SendPacket(acks, true);

                    PendingAcks.Clear();
                }
            }
        }

        /// <summary>
        /// Resend unacknowledged packets
        /// </summary>
        private void ResendUnacked()
        {
            int now = Environment.TickCount;

            lock (NeedAck)
            {
                foreach (Packet packet in NeedAck.Values)
                {
                    if (now - packet.TickCount > Client.Settings.RESEND_TIMEOUT)
                    {
                        try
                        {
                            Client.DebugLog("Resending " + packet.Type.ToString() + " packet (" + packet.Header.Sequence +
                                "), " + (now - packet.TickCount) + "ms have passed");
                            packet.Header.Resent = true;
                            ResentPackets++;
                            SendPacket(packet, false);
                        }
                        catch (Exception ex)
                        {
                            Client.DebugLog("got exception trying to resend packet: " + ex.ToString());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback handler for incomming data
        /// </summary>
        /// <param name="result"></param>
        private void OnReceivedData(IAsyncResult result)
        {
            Packet packet = null;
            int numBytes;

            // Update the disconnect flag so this sim doesn't time out
            DisconnectCandidate = false;

            #region Packet Decoding

            lock (RecvBuffer)
            {
                // Retrieve the incoming packet
                try
                {
                    numBytes = Connection.EndReceiveFrom(result, ref endPoint);

                    int packetEnd = numBytes - 1;
                    packet = Packet.BuildPacket(RecvBuffer, ref packetEnd, ZeroBuffer);

                    Connection.BeginReceiveFrom(RecvBuffer, 0, RecvBuffer.Length, SocketFlags.None, ref endPoint, ReceivedData, null);
                }
                catch (SocketException)
                {
                    Client.Log(endPoint.ToString() + " socket is closed, shutting down " + this.ToString(),
                        Helpers.LogLevel.Info);
                    Network.DisconnectSim(this);
                    return;
                }
            }

            // Fail-safe check
            if (packet == null)
            {
                Client.Log("Couldn't build a message from the incoming data", Helpers.LogLevel.Warning);
                return;
            }

            RecvBytes += (ulong)numBytes;
            RecvPackets++;

            #endregion Packet Decoding

            #region Reliable Handling

            if (packet.Header.Reliable)
            {
                // Queue up ACKs for resent packets
                lock (PendingAcks)
                {
                    uint sequence = (uint)packet.Header.Sequence;
                    if (!PendingAcks.ContainsKey(sequence)) PendingAcks[sequence] = sequence;
                }

                // Send out ACKs if we have a lot of them
                if (PendingAcks.Count >= Client.Settings.MAX_PENDING_ACKS)
                    SendAcks();
            }

            #endregion Reliable Handling

            #region Inbox Tracking

            // The Inbox doesn't serve a functional purpose any more, it's only used for debugging
            lock (Inbox)
            {
                // Check if we already received this packet
                if (Inbox.Contains(packet.Header.Sequence))
                {
                    Client.DebugLog("Received a duplicate " + packet.Type.ToString() + ", sequence=" +
                        packet.Header.Sequence + ", resent=" + ((packet.Header.Resent) ? "Yes" : "No") +
                        ", Inbox.Count=" + Inbox.Count + ", NeedAck.Count=" + NeedAck.Count);

                    // Avoid firing a callback twice for the same packet
                    return;
                }
                else
                {
                    // Keep the Inbox size within a certain capacity
                    while (Inbox.Count >= Client.Settings.INBOX_SIZE)
                    {
                        Inbox.Dequeue(); Inbox.Dequeue();
                        Inbox.Dequeue(); Inbox.Dequeue();
                    }

                    // Add this packet to the inbox
                    Inbox.Enqueue(packet.Header.Sequence);
                }
            }

            #endregion Inbox Tracking

            #region ACK handling

            // Handle appended ACKs
            if (packet.Header.AppendedAcks)
            {
                lock (NeedAck)
                {
                    for (int i = 0; i < packet.Header.AckList.Length; i++)
                        NeedAck.Remove(packet.Header.AckList[i]);
                }
            }

            // Handle PacketAck packets
            if (packet.Type == PacketType.PacketAck)
            {
                PacketAckPacket ackPacket = (PacketAckPacket)packet;

                lock (NeedAck)
                {
                    for (int i = 0; i < ackPacket.Packets.Length; i++)
                        NeedAck.Remove(ackPacket.Packets[i].ID);
                }
            }

            #endregion ACK handling

            #region FireCallbacks

            if (Network.Callbacks.ContainsKey(packet.Type))
            {
                List<NetworkManager.PacketCallback> callbackArray = Network.Callbacks[packet.Type];

                // Fire any registered callbacks
                for (int i = 0; i < callbackArray.Count; i++)
                {
                    if (callbackArray[i] != null)
                    {
                        try { callbackArray[i](packet, this); }
                        catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                    }
                }
            }

            if (Network.Callbacks.ContainsKey(PacketType.Default))
            {
                List<NetworkManager.PacketCallback> callbackArray = Network.Callbacks[PacketType.Default];

                // Fire any registered callbacks
                for (int i = 0; i < callbackArray.Count; i++)
                {
                    if (callbackArray[i] != null)
                    {
                        try { callbackArray[i](packet, this); }
                        catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                    }
                }
            }

            #endregion FireCallbacks

            if (Client.Settings.DEBUG)
            {
                int workerThreads, completionPortThreads;
                ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
                if (workerThreads < 5 || completionPortThreads < 50)
                {
                    Client.Log(String.Format(
                        "Resource starvation approaching, WorkerThreads: {0}, CompletionPortThreads: {1}",
                        workerThreads, completionPortThreads), Helpers.LogLevel.Warning);
                }
            }
        }

        private void AckTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs ea)
        {
            SendAcks();
            ResendUnacked();
        }

        private void StatsTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs ea)
        {
            ulong old_in = 0, old_out = 0;
            if (InBytes.Count >= Client.Settings.STATS_QUEUE_SIZE)
                old_in = InBytes.Dequeue();
            if (OutBytes.Count >= Client.Settings.STATS_QUEUE_SIZE)
                old_out = OutBytes.Dequeue();
            InBytes.Enqueue(RecvBytes);
            OutBytes.Enqueue(SentBytes);
            if (old_in > 0 && old_out > 0)
            {
                IncomingBPS = (int)(RecvBytes - old_in) / Client.Settings.STATS_QUEUE_SIZE;
                OutgoingBPS = (int)(SentBytes - old_out) / Client.Settings.STATS_QUEUE_SIZE;
                /*                              Client.Log("Incoming: "+IncomingBPS +" Out: "+OutgoingBPS +
                                                            " Lag: "+LastLag+" Pings: "+ReceivedPongs +
                                                            "/"+SentPings, Helpers.LogLevel.Debug); 
                */
            }
        }

        private void PingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs ea)
        {
            SendPing();
            SentPings++;
        }
    }
}
