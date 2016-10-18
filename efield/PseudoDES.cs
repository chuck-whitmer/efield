using System;
using System.Collections.Generic;
using System.Text;

namespace Whitmer
{
    class PseudoDES
    {
        static UInt32[] c1 = new UInt32[4]{0xBAA96887,0x1E17D32C,0x03BCDC3C,0x0F33D1B2};
        static UInt32[] c2 = new UInt32[4]{0x4B0F3B58,0xE874F0C3,0x6955C5A6,0x55A7CA46};

        // Method versions of old macros.
        static UInt32 Low16(UInt32 x) {return x & 0xFFFF;}
        static UInt32 High16(UInt32 x) {return x >> 16;}
        static UInt32 Xchg16(UInt32 x) {return (Low16(x) << 16) | High16(x);}
        static UInt32 Low32(UInt64 x) {return (UInt32) x;}
        static UInt32 High32(UInt64 x) { return (UInt32)(x >> 32); }
        static UInt64 Make64(UInt32 lo, UInt32 hi) {return (((UInt64) hi)<<32)+((UInt64)lo);}

        // The seed.
        UInt32 m_iNum;
        UInt32 m_iSeq;

        public PseudoDES()
        {
            m_iNum = 1;
            m_iSeq = 1;
        }

        public PseudoDES(UInt32 iNum, UInt32 iSeq)
        {
            m_iNum = iNum;
            m_iSeq = iSeq;
        }

        public PseudoDES(UInt64 iSeed)
        {
            m_iNum = Low32(iSeed);
            m_iSeq = High32(iSeed);
        }

        public UInt32 Element
        {
            get {return m_iNum;}
        }

        public UInt32 Sequence
        {
            get {return m_iSeq;}
        }
        
        public UInt64 Seed
        {
            get {return Make64(m_iNum,m_iSeq);}
        }
        
        public UInt32 Rand32() // was ul().
        {
            UInt32 kk0,kk1,kk2,iA,iB;

            iA = m_iNum ^ c1[0];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk0 = m_iSeq ^ ((Xchg16(iB) ^ c2[0]) + Low16(iA) * High16(iA));

            iA = kk0 ^ c1[1];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk1 = m_iNum ^ ((Xchg16(iB) ^ c2[1]) + Low16(iA) * High16(iA));

            if (++m_iNum == 0) m_iSeq++;

            iA = kk1 ^ c1[2];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk2 = kk0 ^ ((Xchg16(iB) ^ c2[2]) + Low16(iA) * High16(iA));

            iA = kk2 ^ c1[3];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            return kk1 ^ ((Xchg16(iB) ^ c2[3]) + Low16(iA) * High16(iA));
        }

        public UInt64 Rand64()
        {
            UInt32 kk0, kk1, kk2, kk3, iA, iB;

            iA = m_iNum ^ c1[0];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk0 = m_iSeq ^ ((Xchg16(iB) ^ c2[0]) + Low16(iA) * High16(iA));

            iA = kk0 ^ c1[1];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk1 = m_iNum ^ ((Xchg16(iB) ^ c2[1]) + Low16(iA) * High16(iA));

            iA = kk1 ^ c1[2];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk2 = kk0 ^ ((Xchg16(iB) ^ c2[2]) + Low16(iA) * High16(iA));

            iA = kk2 ^ c1[3];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk3 = kk1 ^ ((Xchg16(iB) ^ c2[3]) + Low16(iA) * High16(iA));
            if (++m_iNum == 0) m_iSeq++;

            return Make64(kk3, kk2);
        }

        static double eMax = (double)(1UL << 62) * 4.0;  // We are looking for a perfect representation of 2.0^64

        public double RandomDouble()
        {
            return Rand64() / eMax;
        }

        public static UInt64 Hash64(UInt64 index)
        {
            UInt32 kk0, kk1, kk2, kk3, iA, iB;

            iA = Low32(index) ^ c1[0];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk0 = High32(index) ^ ((Xchg16(iB) ^ c2[0]) + Low16(iA) * High16(iA));

            iA = kk0 ^ c1[1];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk1 = Low32(index) ^ ((Xchg16(iB) ^ c2[1]) + Low16(iA) * High16(iA));

            iA = kk1 ^ c1[2];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk2 = kk0 ^ ((Xchg16(iB) ^ c2[2]) + Low16(iA) * High16(iA));

            iA = kk2 ^ c1[3];
            iB = Low16(iA) * Low16(iA) + ~(High16(iA) * High16(iA));
            kk3 = kk1 ^ ((Xchg16(iB) ^ c2[3]) + Low16(iA) * High16(iA));

            return Make64(kk3, kk2);
        }

        static UInt32[,] testData = new UInt32[4, 4]
                                        {
                                            { 1, 1,0x604D1DCE,0x509C0C23},
                                            { 1,99,0xD97F8571,0xA66CB41A},
                                            {99, 1,0x7822309D,0x64300984},
                                            {99,99,0xD7F376F0,0x59BA89EB}
                                        };

        public bool Test()
        {
            bool bSuccess = true;

            for (int ii=0; ii<4; ii++)
            {
                bSuccess &= (Hash64(Make64(testData[ii, 1], testData[ii, 0])) == Make64(testData[ii, 3], testData[ii, 2]));
            }

            UInt32 iSaveSeq = m_iSeq;
            UInt32 iSaveNum = m_iNum;

            for (int ii=0; ii<4; ii++)
            {
                m_iSeq = testData[ii,0]; m_iNum = testData[ii,1];
                bSuccess &= (Rand32() == testData[ii,3]);
            }

            m_iSeq = iSaveSeq;
            m_iNum = iSaveNum;
            return bSuccess;
        }

    }
}
