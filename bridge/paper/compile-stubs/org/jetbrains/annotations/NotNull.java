package org.jetbrains.annotations;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

/** Paper API 컴파일에만 사용하는 애너테이션 자리표시자입니다. 배포 JAR에는 포함하지 않습니다. */
@Retention(RetentionPolicy.CLASS)
@Target({ElementType.METHOD, ElementType.PARAMETER, ElementType.FIELD, ElementType.TYPE_USE})
public @interface NotNull {
}
