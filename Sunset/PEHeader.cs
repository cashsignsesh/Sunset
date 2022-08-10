﻿/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 5/13/2021
 * Time: 12:06 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Sunset {
	
	//bad use of struct sorta but w.e microsoft does the same
	[StructLayout(LayoutKind.Sequential)]
	unsafe public struct PEHeader {
		
		#region MZ header
		
		public UInt16
			
			e_magic, e_cblp, e_cp, e_crlc, e_cparhdr, e_minalloc, e_maxalloc,
		    e_ss, e_sp, e_csum, e_ip, e_cs, e_lsarlc, e_ovno, e_res, e_res_0,
		    e_res_1, e_res_2, e_oemid, e_oeminfo, e_res2, e_res2_0, e_res2_1, 
		    e_res2_3,e_res2_2,e_res2_4,e_res2_5,e_res2_6, e_res2_7, e_res2_8;
		
		public UInt32 e_lfanew;
		
		#endregion
		
		#region DOS stub
		
		public fixed Byte unknown[14],
					      msg[38], // ASCII str
					      unknown_0[5],
					      unknown_empty[7];
		
		#endregion
		
		#region PE header
		
		public UInt32 signature;
		
		public Machine machine;
		public UInt16 numberOfSections;
		
		public UInt32 
			
			timeDateStamp,
			pointerToSymbolTable,
			numberOfSymbols;
			
		public UInt16
			
			sizeOfOptionalHeader,
			characteristics;//First characteristics (Pe header one)
		
		#endregion
		
		#region PE optional header
		
		public UInt16 magic;
		
		public Byte 
			
			majorLinkerVersion,
			minorLinkerVersion;
			
		public UInt32
			
			sizeOfCode,
			sizeOfInitializedData,
			sizeOfUninitializedData,
			addressOfEntryPoint,
			baseOfCode,
			baseOfData,
			imageBase,
			sectionAlignment,
			fileAlignment;
			
		public UInt16
			
			majorOperatingSystemVersion,
			minorOperatingSystemVersion,
			majorImageVersion,
			minorImageVersion,
			majorSubsystemVersion,
			minorSubsystemVersion;
			
		public UInt32
			
			win32VersionValue,
			sizeOfImage,
			sizeOfHeaders,
			checkSum;
			
		public UInt16
			
			subSystem,
			dllCharacteristics;
			
		public UInt32
			
			sizeOfStackReserve,
			sizeOfStackCommit,
			sizeOfHeapReserve,
			sizeOfHeapCommit,
			loaderFlags,
			numberOfRvaAndSizes;
		
		#endregion
		
		#region Data directories
		
		public fixed UInt32 dir[32];
		
		#endregion
		
		#region PE code section
		
		public fixed Byte name[8];
		
		public UInt32 
			
			virtualSize,
			virtualAddress,
			sizeOfRawData,
			pointerToRawData,
			pointerToRelocations,
			pointerToLinenumbers;
			
		public UInt16
			
			numberOfRelocations,
			numberOfLinenumbers;
			
		public UInt32 characteristics_0; //Second characteristics (PE code section one)
		
		#endregion
		
		#region Import/data table
		
		public fixed UInt64 tableBytes[12];

        #endregion

    }

    public static class PEHeaderFactory {

        public static UInt32 latestDataSectAddr;
        public const UInt32 dataSectAddr=0x00403000;
        // ^ when fixing this
        // when a child parser is done compiling, give the index of
        // opcodes that need to be changed to the actual data sect
        // address. Update the index relative to the main parser,
        // and update all opcode indexes to be set to the valid data sect addr
        // plus the old original value.
		
		unsafe static public PEHeader newHdr (List<Byte> opcodes,List<Byte> importOpcodes,UInt32 endMemAddress,Int32 offset,UInt32 importOpcodesVirtualSize,Boolean gui=false) {
			
			PEHeader hdr=default(PEHeader);
			
			List<Byte>mockOpcodes=new List<Byte>(opcodes);
			while (mockOpcodes.Count%512!=0)
				mockOpcodes.Add(0x00);
			
			UInt16 sections=(UInt16)((importOpcodes!=null)?2:1);
            if (Parser.dataSectBytes.Count()!=0) ++sections; // (Data Section)
			
			#region MZ header
			
			hdr.e_magic=BitConverter.ToUInt16(Encoding.ASCII.GetBytes("MZ"),0);
			hdr.e_cblp=(UInt16)(opcodes.Count%512);
			hdr.e_cp=(UInt16)(Math.Ceiling((Decimal)((Decimal)opcodes.Count/512M)));
			const UInt16 mzHeaderSize=(UInt16)(64/16);
			hdr.e_cparhdr=mzHeaderSize;
			hdr.e_minalloc=0x0010;
			hdr.e_maxalloc=UInt16.MaxValue;
			hdr.e_sp=0x0140;
			hdr.e_lsarlc=0x0040;
			const Byte MZheader_ByteSize=64,DOSStub_ByteSize=64;
			hdr.e_lfanew=(UInt32)(MZheader_ByteSize+DOSStub_ByteSize);
			
			#endregion
			
			#region DOS stub
			
			Marshal.Copy(new Byte[]{0x0E,0x1F,0xBA,0x0E,0,0xB4,9,0xCD,0x21,0xB8,1,0x4C,0xCD,0x21},0,new IntPtr(hdr.unknown),14);
			const String msg="This program cannot be run in DOS mode";
			Marshal.Copy(msg.Select(x=>(Byte)x).ToArray(),0,new IntPtr(hdr.msg),msg.Length);
			Marshal.Copy(new Byte[]{0x2E,0x0D,0x0D,0x0A,0x24},0,new IntPtr(hdr.unknown_0),5);
			
			#endregion
			
			#region PE header
			
			hdr.signature=BitConverter.ToUInt16(Encoding.ASCII.GetBytes("PE"),0);
			hdr.machine=Machine.I386;
			hdr.numberOfSections=sections;
			hdr.timeDateStamp=(UInt32)((DateTime.UtcNow.Subtract(new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc))).TotalSeconds);
			hdr.sizeOfOptionalHeader=0x00E0;
			hdr.characteristics=0x010F;
			
			#endregion
			
			#region PE Optional Header
			
			hdr.magic=0x010B;//IMAGE_NT_OPTIONAL_HDR32_MAGIC
			hdr.majorLinkerVersion=1;
			hdr.minorLinkerVersion=0x49;
			hdr.sizeOfCode=(UInt32)mockOpcodes.Count;
			hdr.sizeOfInitializedData=(UInt32)(mockOpcodes.Count+(importOpcodes!=null?importOpcodes.Count:0));
			const UInt32 alignment=0x00001000,imgBase=0x00400000;
			hdr.addressOfEntryPoint=alignment;
			hdr.baseOfCode=alignment;
			hdr.baseOfData=alignment;
			hdr.imageBase=imgBase;
			hdr.sectionAlignment=alignment;
			hdr.fileAlignment=(UInt32)512;
			hdr.majorOperatingSystemVersion=(UInt16)1;
			hdr.minorSubsystemVersion=(UInt16)0x000A;
			hdr.majorSubsystemVersion=(UInt16)3;
			UInt32 opcodesSectSize=(UInt32)(opcodes.Count-(opcodes.Count%alignment)+alignment),importOpcodesSectSize=0,dataOpcodesSectSize=0;
			hdr.sizeOfImage=opcodesSectSize+alignment;//+aligment at end to account for header
			if (importOpcodes!=null) {
				importOpcodesSectSize=(UInt32)(((importOpcodes.Count-(importOpcodes.Count%alignment)+alignment)));
				hdr.sizeOfImage+=importOpcodesSectSize;
			}
			hdr.sizeOfHeaders=(UInt32)Marshal.SizeOf(typeof(PEHeader));
			hdr.subSystem=(UInt16)((gui)?2:3);//IMAGE_SUBSYSTEM_WINDOWS_GUI,IMAGE_SUBSYSTEM_WINDOWS_CUI
			hdr.sizeOfStackCommit=alignment;
			hdr.sizeOfStackReserve=alignment;
			hdr.sizeOfHeapReserve=alignment*16;
			hdr.numberOfRvaAndSizes=16;
			
			#endregion
			
			#region PE Code Section
			
			Marshal.Copy(".sunset".toCodeSectNameBytes(),0,new IntPtr(hdr.name),8);
			hdr.virtualSize=(UInt32)(opcodes.Count);
			hdr.virtualAddress=alignment;
			hdr.sizeOfRawData=(UInt32)mockOpcodes.Count;
			hdr.pointerToRawData=512;
			hdr.characteristics_0=0xE0000060;

            #endregion

            #region Sections

            offset=0;
            
            #region Import section

            if (importOpcodes!=null) {
				
				UInt32 addr=opcodesSectSize+alignment;
				
				hdr.dir[2]=addr;
				hdr.dir[3]=importOpcodesVirtualSize;
				hdr.tableBytes[0]=BitConverter.ToUInt64(Encoding.ASCII.GetBytes(".idata").Concat(new Byte[]{0,0}).ToArray(),0);
                hdr.tableBytes[1]=BitConverter.ToUInt64(BitConverter.GetBytes(importOpcodesVirtualSize).Concat(BitConverter.GetBytes(addr)).ToArray(),0);
				//importTableBytes[2] - First UInt32: Size of Import Section Raw Data, Second UInt32: Pointer to Import Section Raw Data (The amount of bytes until the import section)
				hdr.tableBytes[2]=BitConverter.ToUInt64(BitConverter.GetBytes((UInt32)importOpcodes.Count).Concat(BitConverter.GetBytes((UInt32)Marshal.SizeOf(typeof(PEHeader))+mockOpcodes.Count)).ToArray(),0);
				hdr.tableBytes[4]=BitConverter.ToUInt64(new Byte[]{0,0,0,0,0x40,0,0,0xC0},0); // Characteristics == 0C0000040h
				
                offset=5;

			}

            #endregion

            #region Data section

            if (Parser.dataSectBytes.Count()!=0) {

                dataOpcodesSectSize=(UInt32)(Parser.dataSectBytes.Count-(Parser.dataSectBytes.Count%alignment)+alignment);
                hdr.sizeOfImage+=dataOpcodesSectSize;

                UInt32 addr=importOpcodesSectSize+opcodesSectSize+alignment;
                latestDataSectAddr=addr+imgBase;

                // (The data section in Sunset is solely used for storing static instances)
                hdr.tableBytes[0+offset]=BitConverter.ToUInt64(Encoding.ASCII.GetBytes(".data").Concat(new Byte[]{0,0,0 }).ToArray(),0);
                hdr.tableBytes[1+offset]=BitConverter.ToUInt64(BitConverter.GetBytes(Parser.dataSectBytes.Count).Concat(BitConverter.GetBytes(addr)).ToArray(),0);
                while (Parser.dataSectBytes.Count%512!=0)
                    Parser.dataSectBytes.Add(0);
                hdr.tableBytes[2+offset]=BitConverter.ToUInt64((BitConverter.GetBytes((UInt32)Parser.dataSectBytes.Count).Concat(BitConverter.GetBytes((UInt32)Marshal.SizeOf(typeof(PEHeader))+mockOpcodes.Count+(importOpcodes==null?0:importOpcodes.Count)))).ToArray(),0);
                hdr.tableBytes[4+offset]=BitConverter.ToUInt64(new Byte[]{0,0,0,0,0x40,0,0,0xC0},0); // Characteristics == 0C0000040h

            }

            #endregion

            #endregion

            return hdr;
			
		}
		
		static public Byte[] toBytes (this PEHeader hdr) {
			
			const UInt16 hdrSize=512;
			
			Byte[]arr=new Byte[hdrSize];
			IntPtr ptr=Marshal.AllocHGlobal(hdrSize);
			
			Marshal.StructureToPtr(hdr,ptr,true);
			Marshal.Copy(ptr,arr,0,hdrSize);
			Marshal.FreeHGlobal(ptr);
			
			return arr;
			
		}
		
		/// <returns>8 bytes always</returns>
		static private Byte[] toCodeSectNameBytes (this String str) {
			
			const Byte maxLength=8;
			
			if (str.Length>maxLength) throw new Exception("Name too long");
			else if (!str.StartsWith(".")) throw new Exception("Invalid name");

			List<Byte> bytes=str.Select(x=>(Byte)x).ToList();
			bytes.AddRange(new Byte[maxLength-str.Length]);
			return bytes.ToArray();
			
		}
		
	}
	
}
