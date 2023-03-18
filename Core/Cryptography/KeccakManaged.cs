// The SHA3 doesn't create .Net Standard package.
// This is a copy of https://bitbucket.org/jdluzen/sha3/raw/d1fd55dc225d18a7fb61515b62d3c8f164d2e788/SHA3Managed/SHA3Managed.cs

using System;

namespace IpfsShipyard.Ipfs.Core.Cryptography;

internal class KeccakManaged : Keccak
{
    public KeccakManaged(int hashBitLength)
        : base(hashBitLength)
    {
    }

    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        base.HashCore(array, ibStart, cbSize);
        if (cbSize == 0)
            return;
        var sizeInBytes = SizeInBytes;
        Buffer ??= new byte[sizeInBytes];
        var stride = sizeInBytes >> 3;
        var utemps = new ulong[stride];
        if (BuffLength == sizeInBytes)
            throw new("Unexpected error, the internal buffer is full");
        AddToBuffer(array, ref ibStart, ref cbSize);
        if (BuffLength == sizeInBytes)//buffer full
        {
            System.Buffer.BlockCopy(Buffer, 0, utemps, 0, sizeInBytes);
            KeccakF(utemps, stride);
            BuffLength = 0;
        }
        for (; cbSize >= sizeInBytes; cbSize -= sizeInBytes, ibStart += sizeInBytes)
        {
            System.Buffer.BlockCopy(array, ibStart, utemps, 0, sizeInBytes);
            KeccakF(utemps, stride);
        }
        if (cbSize > 0)//some left over
        {
            System.Buffer.BlockCopy(array, ibStart, Buffer, BuffLength, cbSize);
            BuffLength += cbSize;
        }
    }

    protected override byte[] HashFinal()
    {
        var sizeInBytes = SizeInBytes;
        var outb = new byte[HashByteLength];
        //    padding
        if (Buffer == null)
            Buffer = new byte[sizeInBytes];
        else
            Array.Clear(Buffer, BuffLength, sizeInBytes - BuffLength);
        Buffer[BuffLength++] = 1;
        Buffer[sizeInBytes - 1] |= 0x80;
        var stride = sizeInBytes >> 3;
        var utemps = new ulong[stride];
        System.Buffer.BlockCopy(Buffer, 0, utemps, 0, sizeInBytes);
        KeccakF(utemps, stride);
        System.Buffer.BlockCopy(state, 0, outb, 0, HashByteLength);
        return outb;
    }

    private void KeccakF(ulong[] inb, int laneCount)
    {
        while (--laneCount >= 0)
            state[laneCount] ^= inb[laneCount];
        ulong bCa, bCe, bCi, bCo, bCu;
        ulong da, de, di, @do, du;
        ulong eba, ebe, ebi, ebo, ebu;
        ulong ega, ege, egi, ego, egu;
        ulong eka, eke, eki, eko, eku;
        ulong ema, eme, emi, emo, emu;
        ulong esa, ese, esi, eso, esu;
        var round = laneCount;

        //copyFromState(A, state)
        var aba = state[0];
        var abe = state[1];
        var abi = state[2];
        var abo = state[3];
        var abu = state[4];
        var aga = state[5];
        var age = state[6];
        var agi = state[7];
        var ago = state[8];
        var agu = state[9];
        var aka = state[10];
        var ake = state[11];
        var aki = state[12];
        var ako = state[13];
        var aku = state[14];
        var ama = state[15];
        var ame = state[16];
        var ami = state[17];
        var amo = state[18];
        var amu = state[19];
        var asa = state[20];
        var ase = state[21];
        var asi = state[22];
        var aso = state[23];
        var asu = state[24];

        for (round = 0; round < KeccakNumberOfRounds; round += 2)
        {
            //    prepareTheta
            bCa = aba ^ aga ^ aka ^ ama ^ asa;
            bCe = abe ^ age ^ ake ^ ame ^ ase;
            bCi = abi ^ agi ^ aki ^ ami ^ asi;
            bCo = abo ^ ago ^ ako ^ amo ^ aso;
            bCu = abu ^ agu ^ aku ^ amu ^ asu;

            //thetaRhoPiChiIotaPrepareTheta(round  , A, E)
            da = bCu ^ Rol(bCe, 1);
            de = bCa ^ Rol(bCi, 1);
            di = bCe ^ Rol(bCo, 1);
            @do = bCi ^ Rol(bCu, 1);
            du = bCo ^ Rol(bCa, 1);

            aba ^= da;
            bCa = aba;
            age ^= de;
            bCe = Rol(age, 44);
            aki ^= di;
            bCi = Rol(aki, 43);
            amo ^= @do;
            bCo = Rol(amo, 21);
            asu ^= du;
            bCu = Rol(asu, 14);
            eba = bCa ^ (~bCe & bCi);
            eba ^= RoundConstants[round];
            ebe = bCe ^ (~bCi & bCo);
            ebi = bCi ^ (~bCo & bCu);
            ebo = bCo ^ (~bCu & bCa);
            ebu = bCu ^ (~bCa & bCe);

            abo ^= @do;
            bCa = Rol(abo, 28);
            agu ^= du;
            bCe = Rol(agu, 20);
            aka ^= da;
            bCi = Rol(aka, 3);
            ame ^= de;
            bCo = Rol(ame, 45);
            asi ^= di;
            bCu = Rol(asi, 61);
            ega = bCa ^ (~bCe & bCi);
            ege = bCe ^ (~bCi & bCo);
            egi = bCi ^ (~bCo & bCu);
            ego = bCo ^ (~bCu & bCa);
            egu = bCu ^ (~bCa & bCe);

            abe ^= de;
            bCa = Rol(abe, 1);
            agi ^= di;
            bCe = Rol(agi, 6);
            ako ^= @do;
            bCi = Rol(ako, 25);
            amu ^= du;
            bCo = Rol(amu, 8);
            asa ^= da;
            bCu = Rol(asa, 18);
            eka = bCa ^ (~bCe & bCi);
            eke = bCe ^ (~bCi & bCo);
            eki = bCi ^ (~bCo & bCu);
            eko = bCo ^ (~bCu & bCa);
            eku = bCu ^ (~bCa & bCe);

            abu ^= du;
            bCa = Rol(abu, 27);
            aga ^= da;
            bCe = Rol(aga, 36);
            ake ^= de;
            bCi = Rol(ake, 10);
            ami ^= di;
            bCo = Rol(ami, 15);
            aso ^= @do;
            bCu = Rol(aso, 56);
            ema = bCa ^ (~bCe & bCi);
            eme = bCe ^ (~bCi & bCo);
            emi = bCi ^ (~bCo & bCu);
            emo = bCo ^ (~bCu & bCa);
            emu = bCu ^ (~bCa & bCe);

            abi ^= di;
            bCa = Rol(abi, 62);
            ago ^= @do;
            bCe = Rol(ago, 55);
            aku ^= du;
            bCi = Rol(aku, 39);
            ama ^= da;
            bCo = Rol(ama, 41);
            ase ^= de;
            bCu = Rol(ase, 2);
            esa = bCa ^ (~bCe & bCi);
            ese = bCe ^ (~bCi & bCo);
            esi = bCi ^ (~bCo & bCu);
            eso = bCo ^ (~bCu & bCa);
            esu = bCu ^ (~bCa & bCe);

            //    prepareTheta
            bCa = eba ^ ega ^ eka ^ ema ^ esa;
            bCe = ebe ^ ege ^ eke ^ eme ^ ese;
            bCi = ebi ^ egi ^ eki ^ emi ^ esi;
            bCo = ebo ^ ego ^ eko ^ emo ^ eso;
            bCu = ebu ^ egu ^ eku ^ emu ^ esu;

            //thetaRhoPiChiIotaPrepareTheta(round+1, E, A)
            da = bCu ^ Rol(bCe, 1);
            de = bCa ^ Rol(bCi, 1);
            di = bCe ^ Rol(bCo, 1);
            @do = bCi ^ Rol(bCu, 1);
            du = bCo ^ Rol(bCa, 1);

            eba ^= da;
            bCa = eba;
            ege ^= de;
            bCe = Rol(ege, 44);
            eki ^= di;
            bCi = Rol(eki, 43);
            emo ^= @do;
            bCo = Rol(emo, 21);
            esu ^= du;
            bCu = Rol(esu, 14);
            aba = bCa ^ (~bCe & bCi);
            aba ^= RoundConstants[round + 1];
            abe = bCe ^ (~bCi & bCo);
            abi = bCi ^ (~bCo & bCu);
            abo = bCo ^ (~bCu & bCa);
            abu = bCu ^ (~bCa & bCe);

            ebo ^= @do;
            bCa = Rol(ebo, 28);
            egu ^= du;
            bCe = Rol(egu, 20);
            eka ^= da;
            bCi = Rol(eka, 3);
            eme ^= de;
            bCo = Rol(eme, 45);
            esi ^= di;
            bCu = Rol(esi, 61);
            aga = bCa ^ (~bCe & bCi);
            age = bCe ^ (~bCi & bCo);
            agi = bCi ^ (~bCo & bCu);
            ago = bCo ^ (~bCu & bCa);
            agu = bCu ^ (~bCa & bCe);

            ebe ^= de;
            bCa = Rol(ebe, 1);
            egi ^= di;
            bCe = Rol(egi, 6);
            eko ^= @do;
            bCi = Rol(eko, 25);
            emu ^= du;
            bCo = Rol(emu, 8);
            esa ^= da;
            bCu = Rol(esa, 18);
            aka = bCa ^ (~bCe & bCi);
            ake = bCe ^ (~bCi & bCo);
            aki = bCi ^ (~bCo & bCu);
            ako = bCo ^ (~bCu & bCa);
            aku = bCu ^ (~bCa & bCe);

            ebu ^= du;
            bCa = Rol(ebu, 27);
            ega ^= da;
            bCe = Rol(ega, 36);
            eke ^= de;
            bCi = Rol(eke, 10);
            emi ^= di;
            bCo = Rol(emi, 15);
            eso ^= @do;
            bCu = Rol(eso, 56);
            ama = bCa ^ (~bCe & bCi);
            ame = bCe ^ (~bCi & bCo);
            ami = bCi ^ (~bCo & bCu);
            amo = bCo ^ (~bCu & bCa);
            amu = bCu ^ (~bCa & bCe);

            ebi ^= di;
            bCa = Rol(ebi, 62);
            ego ^= @do;
            bCe = Rol(ego, 55);
            eku ^= du;
            bCi = Rol(eku, 39);
            ema ^= da;
            bCo = Rol(ema, 41);
            ese ^= de;
            bCu = Rol(ese, 2);
            asa = bCa ^ (~bCe & bCi);
            ase = bCe ^ (~bCi & bCo);
            asi = bCi ^ (~bCo & bCu);
            aso = bCo ^ (~bCu & bCa);
            asu = bCu ^ (~bCa & bCe);
        }

        //copyToState(state, A)
        state[0] = aba;
        state[1] = abe;
        state[2] = abi;
        state[3] = abo;
        state[4] = abu;
        state[5] = aga;
        state[6] = age;
        state[7] = agi;
        state[8] = ago;
        state[9] = agu;
        state[10] = aka;
        state[11] = ake;
        state[12] = aki;
        state[13] = ako;
        state[14] = aku;
        state[15] = ama;
        state[16] = ame;
        state[17] = ami;
        state[18] = amo;
        state[19] = amu;
        state[20] = asa;
        state[21] = ase;
        state[22] = asi;
        state[23] = aso;
        state[24] = asu;

    }
}