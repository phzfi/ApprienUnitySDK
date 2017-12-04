	/**
	 *
	 * Parses the base SKU name (used by the game to display the graphic assets and 
	 * to deliver the goods to the player) based on the Apprien response (variant SKU)
	 *
	 * Variant SKU is e.g. z_base_sku_name.apprien_500_dfa3, where
	 * - the prefix is z_ (2 characters) to sort the skus on store listing to then end
	 * - followed by the base sku name that can be parsed by splitting the string by separator ".apprien_"
	 * - followed by the price in cents
	 * - followed by 4 character hash
	 * @param string SKU name in (Google/Apple) store, variant or base name (from Apprien Game API)
         * @param string SKU base name in (Google/Apple) store
	 */
	function getBaseSku(storeSku) {
		//first check if this is a variant sku or base sku
		var result = storeSku;
		//first check if this is a variant sku or base sku
		var apprienSeparatorPosition = storeSku.indexOf(".apprien_");
		if (apprienSeparatorPosition > 0) {
			result = storeSku.substring(2, storeSku.length -1);
			result = result.substring(0, result.length - apprienSeparatorPosition);
		}
		return result;
	}

