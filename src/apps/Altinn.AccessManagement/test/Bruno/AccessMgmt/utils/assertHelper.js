// test/helpers/assertHelper.js

/**
 * Normalize an array of strings to lowercase
 */
function toLowerArray(arr = []) {
  return arr.map(v =>
    typeof v === 'string' ? v.toLowerCase() : v
  );
}

/**
 * Case-insensitive version of assert.includeMembers
 */
function assertIncludeMembersIgnoreCase(actual, expected, message) {
  assert.includeMembers(
    toLowerArray(actual),
    toLowerArray(expected),
    message
  );
}

module.exports = {
  toLowerArray,
  assertIncludeMembersIgnoreCase
};
