﻿/*
 * Created by SharpDevelop.
 * User: Elite
 * Date: 6/8/2021
 * Time: 2:22 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProgrammingLanguageTutorialIdea.Keywords {
	
	public class KWFunction : Keyword {
		
		public const String constName="func";
		
		public KWFunction () : base (constName,KeywordType.NATIVE_CALL,true) { }
		
		public override KeywordResult execute (Parser sender,String[] @params) {
			
			if (sender.inFunction)
				throw new ParsingError("Tried to make a function inside of a function");
			
			UInt32 pos=sender.getOpcodesCount()+1;
			sender.addBytes(new Byte[]{0xE9,0,0,0,0});
//			Byte[]newOpcodes=new Byte[]{0xE9,0,0,0,0}; //JMP TO MEM ADDR
			Byte[] newOpcodes=new Byte[0],endOpcodes=(@params.Length==0)?new Byte[]{0xC3}/*RET*/:new Byte[]{0xC2/*RET SHORT:(STACK RESTORATION AMOUNT)*/}.Concat(BitConverter.GetBytes((UInt16)(@params.Length*4))).ToArray();
			Block functionBlock=new Block(delegate{sender.inFunction=false;},sender.memAddress,endOpcodes,true);
			sender.addBlock(functionBlock,0);
			sender.blocksMemPositions[functionBlock].Add(pos);
			sender.inFunction=true;
			sender.nextFunctionParamsCount=(UInt16)@params.Length;
			sender.lastFunctionBlock=functionBlock;
			List<Tuple<String,VarType>>paramTypes=new List<Tuple<String,VarType>>();
			UInt16 paramIndex=0;
			foreach (String s in @params) {
				
				String[]split=s.Split(' ');
				if (split.Length!=2)
					throw new ParsingError("Invalid function declaration parameter: \""+s+'"');
				
				String unparsedType=split[0],varName=split[1];
				
				Tuple<String,VarType>varType=sender.getVarType(unparsedType);
				paramTypes.Add(varType);
				Console.WriteLine(varType.Item1+','+varType.Item2.ToString()+','+varName);
				functionBlock.localVariables.Add(varName,new Tuple<Tuple<String,VarType>,SByte>(varType,(SByte)((paramIndex*4)+8)));
				++paramIndex;
				
			}
			sender.nextFunctionParamTypes=paramTypes.ToArray();
			
			return new KeywordResult(){newStatus=ParsingStatus.SEARCHING_FUNCTION_NAME,newOpcodes=newOpcodes};
		
		}
		
	}
	
}
