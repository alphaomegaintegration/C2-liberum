/////////////////////////////////////////////////////////////////////////////
// project/card/src/aws/card/CallToCEEDays.java
// vim: ts=4 sts=4 sw=4
// THIS FILE WAS AUTO-GENERATED ON Thu Apr 16 08:02:31 AM MST 2026.
// Build: DEBUG

/////////////////////////////////////////////////////////////////////////////
// CALL TO CEEDAYS

/////////////////////////////////////////////////////////////////////////////
// Copyright Amazon.com, Inc. or its affiliates.
// All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
// either express or implied. See the License for the specific
// language governing permissions and limitations under the License

package aws.card;

public class CallToCEEDays {

	// « Static Class Members »
	private static final String MESSAGE_FMT = "%-4sMesgCode:%-4s %-15s TstDate:%-10s Mask used:%-10s    ";
	private static final String MSG_DATE_IS_VALID = "Date is valid";
	private static final String MSG_INSUFFICIENT = "Insufficient";
	private static final String MSG_DATEVALUE_ERROR = "Datevalue error";
	private static final String MSG_INVALID_ERA = "Invalid Era";
	private static final String MSG_UNSUPP_RANGE = "Unsupp. Range";
	private static final String MSG_INVALID_MONTH = "Invalid month";
	private static final String MSG_BAD_PIC_STRING = "Bad Pic String";
	private static final String MSG_NONNUMERIC_DATA = "Nonnumeric data";
	private static final String MSG_YEARINERA_IS_0 = "YearInEra is 0";
	private static final String MSG_DATE_IS_INVALID = "Date is invalid";

	// « Class Children »
	public static class Args
	{
		String date;
		String dateFormat;
		String result;
	}

	// « Class Global Return Code »
	static int returnCode;

	// « Class Global Call Function »
	public static void call(Args args)
	{
		aws.backend.CEEDays.Args ia = new aws.backend.CEEDays.Args();
		ia.dateToTest = args.date;
		ia.dateFormat = args.dateFormat;
		ia.outputLillian = 0;

		aws.backend.CEEDays.call(ia);

		String result;
		switch (ia.feedbackCode.getMsgNo()) {
			case aws.backend.CEEDays.MSGNO_VALID_DATE:
				result = MSG_DATE_IS_VALID;
				break;
			case aws.backend.CEEDays.MSGNO_INSUFFICIENT_DATA:
				result = MSG_INSUFFICIENT;
				break;
			case aws.backend.CEEDays.MSGNO_BAD_DATE:
				result = MSG_DATEVALUE_ERROR;
				break;
			case aws.backend.CEEDays.MSGNO_INVALID_ERA:
				result = MSG_INVALID_ERA;
				break;
			case aws.backend.CEEDays.MSGNO_UNSUPP_RANGE:
				result = MSG_UNSUPP_RANGE;
				break;
			case aws.backend.CEEDays.MSGNO_INVALID_MONTH:
				result = MSG_INVALID_MONTH;
				break;
			case aws.backend.CEEDays.MSGNO_BAD_PIC_STRING:
				result = MSG_BAD_PIC_STRING;
				break;
			case aws.backend.CEEDays.MSGNO_NON_NUMERIC_DATA:
				result = MSG_NONNUMERIC_DATA;
				break;
			case aws.backend.CEEDays.MSGNO_YEAR_IN_ERA_ZERO:
				result = MSG_YEARINERA_IS_0;
				break;
			default:
				result = MSG_DATE_IS_INVALID;
		}

		args.result = String.format(MESSAGE_FMT,
				ia.feedbackCode.getSeverity(),
				ia.feedbackCode.getMsgNo(),
				result,
				args.date,
				args.dateFormat);

		returnCode = ia.feedbackCode.getSeverity();
	}

	// « Class Main Function »
	public static void main(String[] args) {
		Args ia = new Args();
		ia.date = args[0];
		ia.dateFormat = args[1];
		call(ia);
		System.out.println(ia.result);
	}
}
