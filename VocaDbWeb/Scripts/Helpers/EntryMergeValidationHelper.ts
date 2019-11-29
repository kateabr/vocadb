
import EntryStatus from '../Models/EntryStatus';

//module vdb.helpers {
	
	export default class EntryMergeValidationHelper {

		private static toEnum(statusStr: string | EntryStatus): EntryStatus {
		
			if (typeof statusStr === "string") {
				return EntryStatus[statusStr];
			} else {
				return statusStr;
			}
				 
		}

		public static validate(baseStatus: string | EntryStatus, targetStatus: string | EntryStatus, baseCreated: string, targetCreated: string) {

			var baseStatusEnum = EntryMergeValidationHelper.toEnum(baseStatus);
			var targetStatusEnum = EntryMergeValidationHelper.toEnum(targetStatus);

			return {
				validationError_targetIsLessComplete: moment(targetCreated) <= moment(baseCreated) && targetStatusEnum === EntryStatus.Draft && baseStatusEnum > EntryStatus.Draft,
				validationError_targetIsNewer: !(targetStatusEnum > EntryStatus.Draft && baseStatusEnum === EntryStatus.Draft) && moment(targetCreated) > moment(baseCreated)
			};

		}

		public static validateEntry(base: dc.CommonEntryContract, target: dc.CommonEntryContract) {

			if (base == null || target == null) {
				return {
					validationError_targetIsLessComplete: false,
					validationError_targetIsNewer: false
				}
			}

			return EntryMergeValidationHelper.validate(base.status, target.status, base.createDate, target.createDate);

		}

	}

	export interface IEntryMergeValidationResult {
		validationError_targetIsLessComplete: boolean;
		validationError_targetIsNewer: boolean;
	}

//}