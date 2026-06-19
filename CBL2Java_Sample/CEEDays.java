package aws.backend;

public class CEEDays {

	public static final int MSGNO_VALID_DATE = 0;
	public static final int MSGNO_INSUFFICIENT_DATA = 0x09CB;
	public static final int MSGNO_BAD_DATE = 0x09CC;
	public static final int MSGNO_INVALID_ERA = 0x09CD;
	public static final int MSGNO_UNSUPP_RANGE = 0x09D1;
	public static final int MSGNO_INVALID_MONTH = 0x09D5;
	public static final int MSGNO_BAD_PIC_STRING = 0x09D6;
	public static final int MSGNO_NON_NUMERIC_DATA = 0x09D8;
	public static final int MSGNO_YEAR_IN_ERA_ZERO = 0x09D9;
	
	public static final int CSC_59 = 0x59;
	public static final int FACID_C3C5C5 = 0xC3C5C5;

	public static class FeedbackCode {
		int severity;
		int msgNo;
		int caseSevCtl;
		int facilityId;
		int isInfo;
		
		public FeedbackCode(int msgNo) {
			this.msgNo = msgNo;
		}
		
		public int getSeverity() {
			return severity;
		}
		public int getMsgNo() {
			return msgNo;
		}
	}
	
	public static class Args {

		public String dateToTest;
		public String dateFormat;
		public int outputLillian;
		public FeedbackCode feedbackCode;
	}
	
	public static void call(Args ia) {
		ia.feedbackCode = new FeedbackCode(0);
	}

}
