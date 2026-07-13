package io.github.mcserverlauncher.bridge;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/** 런처와 플러그인이 공유하는 제한된 JSON Lines 도우미입니다. */
final class BridgeProtocol {
    static final int PROTOCOL_VERSION = 1;
    static final int MAXIMUM_LINE_LENGTH = 65536;
    static final int MAXIMUM_SUGGESTIONS = 100;

    private BridgeProtocol() {
    }

    static Map<String, Object> parseObject(String json) {
        if (json == null || json.length() == 0 || json.length() > MAXIMUM_LINE_LENGTH) {
            throw new IllegalArgumentException("invalid-size");
        }
        Object value = new Parser(json).parse();
        if (!(value instanceof Map)) {
            throw new IllegalArgumentException("object-required");
        }
        @SuppressWarnings("unchecked")
        Map<String, Object> object = (Map<String, Object>) value;
        return object;
    }

    static String string(Map<String, Object> value, String key) {
        Object item = value.get(key);
        return item == null ? "" : String.valueOf(item);
    }

    static int integer(Map<String, Object> value, String key) {
        try {
            return Integer.parseInt(string(value, key));
        } catch (NumberFormatException ignored) {
            return 0;
        }
    }

    static String json(Object value) {
        StringBuilder result = new StringBuilder();
        appendJson(result, value);
        if (result.length() > MAXIMUM_LINE_LENGTH) {
            throw new IllegalArgumentException("response-too-large");
        }
        return result.toString();
    }

    private static void appendJson(StringBuilder result, Object value) {
        if (value == null) {
            result.append("null");
        } else if (value instanceof String) {
            appendString(result, (String) value);
        } else if (value instanceof Number || value instanceof Boolean) {
            result.append(value.toString());
        } else if (value instanceof Map) {
            result.append('{');
            boolean first = true;
            for (Map.Entry<?, ?> entry : ((Map<?, ?>) value).entrySet()) {
                if (!first) result.append(',');
                first = false;
                appendString(result, String.valueOf(entry.getKey()));
                result.append(':');
                appendJson(result, entry.getValue());
            }
            result.append('}');
        } else if (value instanceof Iterable) {
            result.append('[');
            boolean first = true;
            for (Object item : (Iterable<?>) value) {
                if (!first) result.append(',');
                first = false;
                appendJson(result, item);
            }
            result.append(']');
        } else {
            appendString(result, String.valueOf(value));
        }
    }

    private static void appendString(StringBuilder result, String value) {
        result.append('"');
        for (int index = 0; index < value.length(); index++) {
            char character = value.charAt(index);
            if (character == '"' || character == '\\') result.append('\\').append(character);
            else if (character == '\n') result.append("\\n");
            else if (character == '\r') result.append("\\r");
            else if (character == '\t') result.append("\\t");
            else if (character < 32) result.append(String.format("\\u%04x", (int) character));
            else result.append(character);
        }
        result.append('"');
    }

    static Map<String, Object> object(Object... values) {
        Map<String, Object> result = new LinkedHashMap<String, Object>();
        for (int index = 0; index + 1 < values.length; index += 2) {
            result.put(String.valueOf(values[index]), values[index + 1]);
        }
        return result;
    }

    private static final class Parser {
        private final String text;
        private int index;

        Parser(String value) {
            text = value;
        }

        Object parse() {
            Object value = readValue();
            skipSpace();
            if (index != text.length()) throw new IllegalArgumentException("trailing-data");
            return value;
        }

        private Object readValue() {
            skipSpace();
            if (index >= text.length()) throw new IllegalArgumentException("unexpected-end");
            char character = text.charAt(index);
            if (character == '{') return readObject();
            if (character == '[') return readArray();
            if (character == '"') return readString();
            if (text.startsWith("true", index)) { index += 4; return Boolean.TRUE; }
            if (text.startsWith("false", index)) { index += 5; return Boolean.FALSE; }
            if (text.startsWith("null", index)) { index += 4; return null; }
            return readNumber();
        }

        private Map<String, Object> readObject() {
            Map<String, Object> result = new LinkedHashMap<String, Object>();
            index++;
            skipSpace();
            if (take('}')) return result;
            while (true) {
                skipSpace();
                String key = readString();
                skipSpace();
                require(':');
                result.put(key, readValue());
                skipSpace();
                if (take('}')) return result;
                require(',');
            }
        }

        private List<Object> readArray() {
            List<Object> result = new ArrayList<Object>();
            index++;
            skipSpace();
            if (take(']')) return result;
            while (true) {
                result.add(readValue());
                skipSpace();
                if (take(']')) return result;
                require(',');
            }
        }

        private String readString() {
            require('"');
            StringBuilder result = new StringBuilder();
            while (index < text.length()) {
                char character = text.charAt(index++);
                if (character == '"') return result.toString();
                if (character != '\\') {
                    result.append(character);
                    continue;
                }
                if (index >= text.length()) throw new IllegalArgumentException("invalid-escape");
                char escaped = text.charAt(index++);
                if (escaped == '"' || escaped == '\\' || escaped == '/') result.append(escaped);
                else if (escaped == 'b') result.append('\b');
                else if (escaped == 'f') result.append('\f');
                else if (escaped == 'n') result.append('\n');
                else if (escaped == 'r') result.append('\r');
                else if (escaped == 't') result.append('\t');
                else if (escaped == 'u' && index + 4 <= text.length()) {
                    result.append((char) Integer.parseInt(text.substring(index, index + 4), 16));
                    index += 4;
                } else throw new IllegalArgumentException("invalid-escape");
            }
            throw new IllegalArgumentException("unterminated-string");
        }

        private Number readNumber() {
            int start = index;
            while (index < text.length() && "-+0123456789.eE".indexOf(text.charAt(index)) >= 0) index++;
            if (start == index) throw new IllegalArgumentException("invalid-value");
            String number = text.substring(start, index);
            try {
				if (number.indexOf('.') >= 0 || number.indexOf('e') >= 0 || number.indexOf('E') >= 0) return Double.valueOf(number);
				return Long.valueOf(number);
            } catch (NumberFormatException exception) {
                throw new IllegalArgumentException("invalid-number", exception);
            }
        }

        private void skipSpace() {
            while (index < text.length() && Character.isWhitespace(text.charAt(index))) index++;
        }

        private boolean take(char expected) {
            if (index < text.length() && text.charAt(index) == expected) { index++; return true; }
            return false;
        }

        private void require(char expected) {
            if (!take(expected)) throw new IllegalArgumentException("expected-" + expected);
        }
    }
}
